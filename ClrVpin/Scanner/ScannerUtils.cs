using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClrVpin.Logging;
using ClrVpin.Models;
using ClrVpin.Models.Scanner;
using ClrVpin.Shared;
using Utils;

namespace ClrVpin.Scanner
{
    public static class ScannerUtils
    {
        private static readonly Models.Settings.Settings _settings = Model.Settings;

        public static List<FileDetail> Check(List<Game> games)
        {
            var unknownFiles = new List<FileDetail>();

            // for each selected check content types
            var checkContentTypes = _settings.GetSelectedCheckContentTypes();

            foreach (var contentType in checkContentTypes)
            {
                // for each content type, match files (from the configured content folder location) with the correct file extension(s) to a table
                var mediaFiles = TableUtils.GetContentFileNames(contentType, contentType.Folder);
                var unmatchedFiles = TableUtils.AssociateContentFilesWithGames(games, mediaFiles, contentType, game => game.Content.ContentHitsCollection.First(contentHits => contentHits.Enum == contentType.Enum));
                unknownFiles.AddRange(unmatchedFiles);

                // identify any unsupported files, i.e. files in the directory that don't have a matching extension
                if (_settings.Scanner.SelectedCheckHitTypes.Contains(HitTypeEnum.Unsupported))
                {
                    var unsupportedFiles = TableUtils.GetUnsupportedMediaFileDetails(contentType, contentType.Folder);

                    // n/a for pinball - since it's expected that extra files will exist in same tables folder
                    // - e.g. vpx, directb2s, pov, ogg, txt, exe, etc
                    if (contentType.Category == ContentTypeCategoryEnum.Media)
                        unknownFiles.AddRange(unsupportedFiles);
                }
            }

            // update each table status as missing if their were no matches
            AddMissingStatus(games);

            return unknownFiles;
        }

        public static async Task<List<FileDetail>> FixAsync(List<Game> games, string backupFolder)
        {
            var fixedFileDetails = await Task.Run(() => Fix(games, backupFolder));
            return fixedFileDetails;
        }

        private static List<FileDetail> Fix(ICollection<Game> games, string backupFolder)
        {
            FileUtils.SetActiveBackupFolder(backupFolder);

            // EVERY GAME THAT HAS A HIT (IRRESPECTIVE OF MATCH CRITERIA) WILL HAVE A GAME FILE RETURNED, i.e. irrespective of whether..
            // - match criteria is selected or relevant
            // - skip options are selected or relevant
            var gameFiles = new List<FileDetail>();

            var selectedContentTypes = _settings.GetSelectedCheckContentTypes();

            // iterate through each selected content type
            foreach (var contentType in selectedContentTypes)
            {
                // fixable game exclude following hit types..
                // - missing - associated with game as the default entry, but can't be fixed.. requires file to be downloaded
                // - unknown - not associated with a game (i.e. no need to check here).. handled elsewhere
                // - unsupported - not associated with any known content type, e.g. Magic.ini
                var fixableContentGames = games.Where(game => game.Content.ContentHitsCollection.Any(contentHits => contentHits.ContentType == contentType && contentHits.Hits.All(hit => hit.Type != HitTypeEnum.Missing))).ToList();

                // fix files associated with games, if they satisfy the fix criteria
                fixableContentGames.ForEach(game =>
                {
                    var gameContentHits = game.Content.ContentHitsCollection.First(contentHits => contentHits.ContentType == contentType);
                    
                    // the underlying HitTypeEnum is declared in descending priority order
                    var orderedByHitType = gameContentHits.Hits.OrderBy(hit => hit.Type);

                    switch (_settings.Scanner.SelectedMultipleMatchOption)
                    {
                        case MultipleMatchOptionEnum.CorrectName:
                            FixOrderedHits(orderedByHitType.ToList(), gameFiles, game);
                            break;
                        case MultipleMatchOptionEnum.LargestSize:
                            FixOrderedHits(orderedByHitType.OrderByDescending(hit => hit.FileInfo.Length).ToList(), gameFiles, game);
                            break;
                        case MultipleMatchOptionEnum.MostRecent:
                            FixOrderedHits(orderedByHitType.OrderByDescending(hit => hit.FileInfo.LastWriteTime).ToList(), gameFiles, game);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });

            }

            // delete empty backup folders - i.e. if there are no files (empty sub-directories are allowed)
            FileUtils.DeleteActiveBackupFolderIfEmpty();

            return gameFiles;
        }

        private static void FixOrderedHits(ICollection<Hit> orderedHits, List<FileDetail> gameFiles, Game game)
        {
            var preferredHit = orderedHits.First();
            var nonPreferredHits = orderedHits.Skip(1).ToList();

            // nothing to fix if the preferred hit is 'correct name' AND there's only 1 hit
            if (preferredHit.Type == HitTypeEnum.CorrectName && orderedHits.Count == 1)
                return;

            // nothing to fix if the preferred hit 'correct name' AND the other hits aren't selected
            // - e.g. preferred = wrong case, other=correct name (not selected)
            if (preferredHit.Type == HitTypeEnum.CorrectName && !nonPreferredHits.Any(hit => hit.Type.In(_settings.Scanner.SelectedFixHitTypes)))
            {
                Logger.Info($"Skipping (fix criteria not selected).. table: {game.GetContentName(_settings.GetContentType(preferredHit.ContentTypeEnum).Category)}, " +
                            $"preferred type: {preferredHit.Type.GetDescription()}, required fix types (unselected): {string.Join('|', nonPreferredHits.Select(x => x.Type.GetDescription()).Distinct())}, " +
                            $"content: {preferredHit.ContentType}, multi option: {_settings.Scanner.SelectedMultipleMatchOption.GetDescription()}");
                return;
            }

            // nothing to fix if the preferred hit isn't selected
            // - e.g. correct name not selected
            if (preferredHit.Type != HitTypeEnum.CorrectName && !preferredHit.Type.In(_settings.Scanner.SelectedFixHitTypes))
            {
                Logger.Info($"Skipping (fix criteria not selected).. table: {game.GetContentName(_settings.GetContentType(preferredHit.ContentTypeEnum).Category)}, " +
                            $"preferred type (unselected): {preferredHit.Type.GetDescription()}, " +
                            $"content: {preferredHit.ContentType}, multi option: {_settings.Scanner.SelectedMultipleMatchOption.GetDescription()}");
                return;
            }

            // delete all hit files except the first
            Logger.Info($"Fixing.. table: {game.GetContentName(_settings.GetContentType(preferredHit.ContentTypeEnum).Category)}, type: {preferredHit.Type.GetDescription()}, content: {preferredHit.ContentType}, multi option: {_settings.Scanner.SelectedMultipleMatchOption.GetDescription()}");
            Logger.Debug($"- matched..\n  src: {FileUtils.GetFileInfoStatistics(preferredHit.Path)}");
            gameFiles.AddRange(FileUtils.DeleteAllExcept(orderedHits, preferredHit, _settings.Scanner.SelectedFixHitTypes));

            // if the preferred hit file isn't 'CorrectName', then rename it
            if (preferredHit.Type != HitTypeEnum.CorrectName)
                gameFiles.Add(FileUtils.Rename(preferredHit, game, _settings.Scanner.SelectedFixHitTypes, _settings.GetContentType(preferredHit.ContentTypeEnum).KindredExtensionsList));
        }

        public static async Task RemoveAsync(List<FileDetail> otherFileDetails)
        {
            await Task.Run(() => Remove(otherFileDetails));
        }

        private static void Remove(List<FileDetail> otherFileDetails)
        {
            // delete files NOT associated with games, i.e. unknown files
            otherFileDetails.ForEach(x =>
            {
                if (x.HitType == HitTypeEnum.Unknown && _settings.Scanner.SelectedFixHitTypes.Contains(HitTypeEnum.Unknown) ||
                    x.HitType == HitTypeEnum.Unsupported && _settings.Scanner.SelectedFixHitTypes.Contains(HitTypeEnum.Unsupported))
                {
                    x.Deleted = true;

                    Logger.Info($"Fixing.. unknown/unsupported file, table: n/a, type: {x.HitType.GetDescription()}, content: n/a");
                    FileUtils.Delete(x.Path, x.HitType, null);
                }
            });
        }

        private static void AddMissingStatus(List<Game> games)
        {
            games.ForEach(game =>
            {
                // add missing content
                game.Content.ContentHitsCollection.ForEach(contentHitCollection =>
                {
                    if (!contentHitCollection.Hits.Any(hit => hit.Type == HitTypeEnum.CorrectName || hit.Type == HitTypeEnum.WrongCase))
                        contentHitCollection.Add(HitTypeEnum.Missing, game.GetContentName(contentHitCollection.ContentType.Category));
                });
            });
        }
    }
}