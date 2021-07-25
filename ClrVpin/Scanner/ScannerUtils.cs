using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClrVpin.Models;
using ClrVpin.Shared;

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

        private static List<FileDetail> Fix(List<Game> games, string backupFolder)
        {
            TableUtils.SetActiveBackupFolder(backupFolder);

            // EVERY GAME THAT HAS A HIT (IRRESPECTIVE OF MATCH CRITERIA) WILL HAVE A GAME FILE RETURNED, i.e. irrespective of whether..
            // - match criteria is selected or relevant
            // - skip options are selected or relevant
            var gameFiles = new List<FileDetail>();

            // fix files associated with games, if they satisfy the fix criteria
            games.ForEach(game =>
            {
                game.Content.ContentHitsCollection.ForEach(contentHitCollection =>
                {
                    if (TableUtils.TryGet(contentHitCollection.Hits, out var hit, HitTypeEnum.Valid))
                    {
                        // valid hit exists.. so delete other hits, i.e. other hits aren't as relevant
                        gameFiles.AddRange(TableUtils.DeleteAllExcept(contentHitCollection.Hits, hit, _settings.Scanner.SelectedFixHitTypes));
                    }
                    else if (TableUtils.TryGet(contentHitCollection.Hits, out hit, HitTypeEnum.WrongCase, HitTypeEnum.TableName, HitTypeEnum.Fuzzy))
                    {
                        // for all 3 hit types.. rename file and delete other entries
                        // - duplicate extension is n/a since it's implied a valid hit already exists, i.e. covered above
                        gameFiles.Add(TableUtils.Rename(hit, game, _settings.Scanner.SelectedFixHitTypes));
                        gameFiles.AddRange(TableUtils.DeleteAllExcept(contentHitCollection.Hits, hit, _settings.Scanner.SelectedFixHitTypes));
                    }

                    // other hit types don't require any additional work..
                    // - unknown - not associated with a game.. handled elsewhere
                    // - missing - can't be fixed.. requires file to be downloaded
                });
            });

            // delete empty backup folders - i.e. if there are no files (empty sub-directories are allowed)
            TableUtils.DeleteActiveBackupFolderIfEmpty();

            return gameFiles;
        }

        public static async Task RemoveAsync(List<FileDetail> otherFileDetails)
        {
            await Task.Run(() => Remove(otherFileDetails));
        }

        public static void Remove(List<FileDetail> otherFileDetails)
        {
            // delete files NOT associated with games, i.e. unknown files
            otherFileDetails.ForEach(x =>
            {
                if (x.HitType == HitTypeEnum.Unknown && _settings.Scanner.SelectedFixHitTypes.Contains(HitTypeEnum.Unknown) ||
                    x.HitType == HitTypeEnum.Unsupported && _settings.Scanner.SelectedFixHitTypes.Contains(HitTypeEnum.Unsupported))
                {
                    x.Deleted = true;
                    TableUtils.Delete(x.Path, x.HitType, null);
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
                    if (!contentHitCollection.Hits.Any(hit => hit.Type == HitTypeEnum.Valid || hit.Type == HitTypeEnum.WrongCase))
                        contentHitCollection.Add(HitTypeEnum.Missing, game.GetContentName(contentHitCollection.ContentType.Category));
                });
            });
        }
    }
}