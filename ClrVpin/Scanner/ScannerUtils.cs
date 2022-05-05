using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClrVpin.Logging;
using ClrVpin.Models;
using ClrVpin.Models.Scanner;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Shared;
using Utils.Extensions;

namespace ClrVpin.Scanner
{
    public static class ScannerUtils
    {
        private static readonly Models.Settings.Settings _settings = Model.Settings;

        public static async Task<List<FileDetail>> CheckAsync(List<Game> games, Action<string, int> updateProgress)
        {
            var unmatchedFiles = await Task.Run(() => Check(games, updateProgress));
            return unmatchedFiles;
        }

        private static List<FileDetail> Check(List<Game> games, Action<string, int> updateProgress)
        {
            var unmatchedFiles = new List<FileDetail>();

            // for each selected check content types
            var checkContentTypes = _settings.GetSelectedCheckContentTypes();

            // retrieve all supported files
            // - for each content type, match files (from the configured content folder location) with the correct file extension(s) to a table
            var contentTypeSupportedFiles = checkContentTypes.Select(contentType => new
            {
                contentType,
                supportedFiles = TableUtils.GetContentFileNames(contentType, contentType.Folder).ToList()
            }).ToList();

            var totalFilesCount = contentTypeSupportedFiles.Sum(details => details.supportedFiles.Count);
            var fileCount = 0;
            contentTypeSupportedFiles.ForEach(details =>
            {
                var supportedFiles = details.supportedFiles;
                var contentType = details.contentType;

                var unknownFiles = TableUtils.AssociateContentFilesWithGames(games, supportedFiles, contentType, game => game.Content.ContentHitsCollection.First(contentHits => contentHits.Enum == contentType.Enum),
                    (fileName, _) => updateProgress($"{contentType.Description}: {fileName}", 100 * ++fileCount / totalFilesCount));
                unmatchedFiles.AddRange(unknownFiles);

                // identify any unsupported files, i.e. files in the directory that don't have a matching extension
                if (_settings.Scanner.SelectedCheckHitTypes.Contains(HitTypeEnum.Unsupported))
                {
                    var unsupportedFiles = TableUtils.GetUnsupportedMediaFileDetails(contentType, contentType.Folder);

                    // n/a for pinball - since it's expected that extra files will exist in same tables folder
                    // - e.g. vpx, directb2s, pov, ogg, txt, exe, etc
                    if (contentType.Category == ContentTypeCategoryEnum.Media)
                        unmatchedFiles.AddRange(unsupportedFiles);
                }
            });

            // update each table status as missing if their were no matches
            AddMissingStatus(games);

            // unmatchedFiles = unknownFiles + unsupportedFiles
            return unmatchedFiles;
        }

        public static async Task<List<FileDetail>> FixAsync(List<Game> games, string backupFolder, Action<string, int> updateProgress)
        {
            var fixedFileDetails = await Task.Run(() => Fix(games, backupFolder, updateProgress));
            return fixedFileDetails;
        }

        private static List<FileDetail> Fix(ICollection<Game> games, string backupFolder, Action<string, int> updateProgress)
        {
            FileUtils.SetActiveBackupFolder(backupFolder);

            // EVERY GAME THAT HAS A HIT (IRRESPECTIVE OF MATCH CRITERIA) WILL HAVE A GAME FILE RETURNED, i.e. irrespective of whether..
            // - match criteria is selected or relevant
            // - skip criteria is selected or relevant
            var gameFiles = new List<FileDetail>();

            var selectedContentTypes = _settings.GetSelectedCheckContentTypes().ToList();

            var gamesWithContentCount = 0;
            var gamesWithContentMaxCount = 0;
            
            static bool GamesWithContentPredicate(Game game, ContentType contentType) => game.Content.ContentHitsCollection.Any(contentHits => contentHits.ContentType == contentType && contentHits.Hits.Any(hit => hit.Type != HitTypeEnum.Missing));

            selectedContentTypes.ForEach(contentType =>
            {
                gamesWithContentMaxCount += games.Count(game => GamesWithContentPredicate(game, contentType));
            });

            // iterate through each selected content type
            selectedContentTypes.ForEach(contentType =>
            {
                // fixable game exclude following hit types..
                // - missing - associated with game as the default entry
                //   - can be fixed if other (non-correct name) matches are available (e.g. fuzzy match( but can't be fixed.. requires file to be downloaded
                //   - if no other matches exist, then the content can't be fixed as the content needs to be downloaded
                // - unknown - not associated with a game (i.e. no need to check here).. handled elsewhere
                // - unsupported - not associated with any known content type, e.g. Magic.ini
                var fixableContentGames = games.Where(game => GamesWithContentPredicate(game, contentType)).ToList();

                // fix files associated with games, if they satisfy the fix criteria
                fixableContentGames.ForEach(game =>
                {
                    updateProgress(game.Description, 100 * ++gamesWithContentCount / gamesWithContentMaxCount);

                    var gameContentHits = game.Content.ContentHitsCollection.First(contentHits => contentHits.ContentType == contentType);

                    // the underlying HitTypeEnum is declared in descending priority order
                    var orderedByHitType = gameContentHits.Hits.OrderBy(hit => hit.Type);

                    // when fixing order, need to cater for no FileInfo, e.g. file missing
                    switch (_settings.Scanner.SelectedMultipleMatchOption)
                    {
                        case MultipleMatchOptionEnum.PreferCorrectName:
                            FixOrderedHits(orderedByHitType.ToList(), gameFiles, game);
                            break;
                        case MultipleMatchOptionEnum.PreferLargestSize:
                            FixOrderedHits(orderedByHitType.OrderByDescending(hit => hit.FileInfo?.Length).ToList(), gameFiles, game);
                            break;
                        case MultipleMatchOptionEnum.PreferMostRecent:
                            FixOrderedHits(orderedByHitType.OrderByDescending(hit => hit.FileInfo?.LastWriteTime).ToList(), gameFiles, game);
                            break;
                        case MultipleMatchOptionEnum.PreferMostRecentAndExceedSizeThreshold:
                            var orderedByMostRecent = orderedByHitType.OrderByDescending(hit => hit.FileInfo?.LastWriteTime).ToList();

                            // if the correct name file exists, then apply additional ordering to filter out (aka de-prioritize) files that don't exceed the threshold
                            decimal? correctHitLength = orderedByMostRecent.FirstOrDefault(x => x.Type == HitTypeEnum.CorrectName)?.FileInfo.Length;
                            if (correctHitLength != null)
                            {
                                var sizeThreshold = _settings.Scanner.MultipleMatchExceedSizeThresholdPercentage / 100;
                                orderedByMostRecent = orderedByMostRecent.OrderByDescending(hit => correctHitLength != 0 && hit.FileInfo?.Length / correctHitLength > sizeThreshold).ToList();
                            }

                            FixOrderedHits(orderedByMostRecent.ToList(), gameFiles, game);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
            });

            // delete empty backup folders - i.e. if there are no files (empty sub-directories are allowed)
            FileUtils.DeleteActiveBackupFolderIfEmpty();

            return gameFiles;
        }

        private static void FixOrderedHits(ICollection<Hit> orderedHits, List<FileDetail> gameFiles, Game game)
        {
            // first hit may be HitType.Missing.. i.e. no file info present
            // - this is filtered out during the file delete/rename/etc because..
            //   a. SelectedFixHitTypes - does not support Missing (yet)
            //   b. Missing files have Size=null
            var preferredHit = orderedHits.First();
            var nonPreferredHits = orderedHits.Skip(1).ToList();

            // nothing to fix if the preferred hit is 'correct name' AND there's only 1 hit
            if (preferredHit.Type == HitTypeEnum.CorrectName && orderedHits.Count == 1)
                return;

            var multiOptionDescription = _settings.Scanner.SelectedMultipleMatchOption.GetDescription();
            if (_settings.Scanner.SelectedMultipleMatchOption == MultipleMatchOptionEnum.PreferMostRecentAndExceedSizeThreshold)
                multiOptionDescription = $"{multiOptionDescription} (criteria: {_settings.Scanner.MultipleMatchExceedSizeThresholdPercentage / 100:P2})";

            // nothing to fix if the preferred hit 'correct name' AND the other hits aren't selected
            // - e.g. preferred = wrong case, other=correct name (not selected)
            if (preferredHit.Type == HitTypeEnum.CorrectName && !nonPreferredHits.Any(hit => hit.Type.In(_settings.Scanner.SelectedFixHitTypes)))
            {
                Logger.Info($"Skipping (fix criteria not selected).. table: {game.Name}, description: {game.Description}, " +
                            $"preferred type: {preferredHit.Type.GetDescription()}, required fix types (unselected): {string.Join('|', nonPreferredHits.Select(x => x.Type.GetDescription()).Distinct())}, " +
                            $"content: {preferredHit.ContentType}, multi option: {multiOptionDescription}");
                return;
            }

            // nothing to fix if the preferred hit isn't selected
            // - e.g. correct name not selected
            if (preferredHit.Type != HitTypeEnum.CorrectName && !preferredHit.Type.In(_settings.Scanner.SelectedFixHitTypes))
            {
                Logger.Info($"Skipping (fix criteria not selected).. table: {game.Name}, description: {game.Description}, " +
                            $"preferred type (unselected): {preferredHit.Type.GetDescription()}, " +
                            $"content: {preferredHit.ContentType}, multi option: {multiOptionDescription}");
                return;
            }

            // delete all hit files except the first
            Logger.Info($"Fixing.. table: {game.Name}, description: {game.Description}, type: {preferredHit.Type.GetDescription()}, content: {preferredHit.ContentType}, multi option: {multiOptionDescription}");

            var (description, warning) = Fuzzy.GetScoreDetail(preferredHit.Score);
            var message = $"- matched (score: {description})..\n  source: {FileUtils.GetFileInfoStatistics(preferredHit.Path)}";
            if (warning)
                Logger.Warn(message);
            else
                Logger.Debug(message);

            gameFiles.AddRange(FileUtils.DeleteAllExcept(orderedHits, preferredHit, _settings.Scanner.SelectedFixHitTypes));

            // if the preferred hit file isn't 'CorrectName', then rename it
            if (preferredHit.Type != HitTypeEnum.CorrectName)
                gameFiles.Add(FileUtils.Rename(preferredHit, game, _settings.Scanner.SelectedFixHitTypes, _settings.GetContentType(preferredHit.ContentTypeEnum).KindredExtensionsList));
        }

        public static async Task RemoveUnmatchedAsync(List<FileDetail> unmatchedFiles, Action<string, int> updateProgress)
        {
            await Task.Run(() => RemoveUnmatched(unmatchedFiles, updateProgress));
        }

        private static void RemoveUnmatched(IEnumerable<FileDetail> unmatchedFiles, Action<string, int> updateProgress)
        {
            // delete files NOT associated with games, aka unmatched files
            var unmatchedFilesToDelete = unmatchedFiles.Where(unmatchedFile =>
                unmatchedFile.HitType == HitTypeEnum.Unknown && _settings.Scanner.SelectedFixHitTypes.Contains(HitTypeEnum.Unknown) ||
                unmatchedFile.HitType == HitTypeEnum.Unsupported && _settings.Scanner.SelectedFixHitTypes.Contains(HitTypeEnum.Unsupported)).ToList();

            unmatchedFilesToDelete.ForEach((fileDetail, i) =>
            {
                updateProgress(Path.GetFileName(fileDetail.Path), 100 * (i+1) / unmatchedFilesToDelete.Count);

                Logger.Info($"Fixing (unmatched).. table: n/a, description: n/a, type: {fileDetail.HitType.GetDescription()}, content: {fileDetail.ContentType.GetDescription()}");
                FileUtils.Delete(fileDetail.Path, fileDetail.HitType, null);

                fileDetail.Deleted = true;
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