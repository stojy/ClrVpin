using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClrVpin.Logging;
using ClrVpin.Models;
using ClrVpin.Shared;

namespace ClrVpin.Scanner
{
    public static class ScannerUtils
    {
        public static List<FixFileDetail> Check(List<Game> games)
        {
            var otherFiles = new List<FixFileDetail>();

            // determine the configured content types
            // - todo; scan non-media content, e.g. tables and b2s
            var checkContentTypes = Model.Config.GetFrontendFolders()
                .Where(x => !x.IsDatabase)
                .Where(type => Model.Config.SelectedCheckContentTypes.Contains(type.Description));

            foreach (var contentType in checkContentTypes)
            {
                // for each content type, match files (from the configured content folder location) with the correct file extension(s) to a table
                var mediaFiles = TableUtils.GetMediaFileNames(contentType, contentType.Folder);
                var unknownMedia = TableUtils.AssociateMediaFilesWithGames(games, mediaFiles, contentType.Enum, game => game.Content.ContentHitsCollection.First(contentHits => contentHits.Type == contentType.Enum));
                otherFiles.AddRange(unknownMedia);

                // identify any unsupported files, i.e. files in the directory that don't have a matching extension
                if (Model.Config.SelectedCheckHitTypes.Contains(HitTypeEnum.Unsupported))
                {
                    var unsupportedFiles = TableUtils.GetUnsupportedMediaFileDetails(contentType, contentType.Folder);
                    otherFiles.AddRange(unsupportedFiles);
                }
            }

            // update each table status as missing if their were no matches
            AddMissingStatus(games);

            return otherFiles;
        }

        public static async Task<List<FixFileDetail>> FixAsync(List<Game> games, List<FixFileDetail> otherFileDetails, string backupFolder)
        {
            var fixedFileDetails = await Task.Run(() => Fix(games, otherFileDetails, backupFolder));
            return fixedFileDetails;
        }

        private static List<FixFileDetail> Fix(List<Game> games, List<FixFileDetail> otherFileDetails, string backupFolder)
        {
            _activeBackupFolder = TableUtils.GetActiveBackupFolder(backupFolder);

            var fixedFileDetails = new List<FixFileDetail>();

            // fix files associated with games, if they satisfy the fix criteria
            games.ForEach(game =>
            {
                game.Content.ContentHitsCollection.ForEach(contentHitCollection =>
                {
                    if (TableUtils.TryGet(contentHitCollection.Hits, out var hit, HitTypeEnum.Valid))
                    {
                        // valid hit exists.. so delete other hits, i.e. other hits aren't as relevant
                        fixedFileDetails.AddRange(TableUtils.DeleteAllExcept(contentHitCollection.Hits, hit, Model.Config.SelectedFixHitTypes));
                    }
                    else if (TableUtils.TryGet(contentHitCollection.Hits, out hit, HitTypeEnum.WrongCase, HitTypeEnum.TableName, HitTypeEnum.Fuzzy))
                    {
                        // for all 3 hit types.. rename file and delete other entries
                        fixedFileDetails.Add(TableUtils.Rename(hit, game, Model.Config.SelectedFixHitTypes));
                        fixedFileDetails.AddRange(TableUtils.DeleteAllExcept(contentHitCollection.Hits, hit, Model.Config.SelectedFixHitTypes));
                    }

                    // other hit types are n/a
                    // - duplicate extension - already taken care as a valid hit will exist
                    // - unknown - not associated with a game.. handled elsewhere
                    // - missing - can't be fixed.. requires file to be downloaded
                });
            });

            // delete files NOT associated with games, i.e. unknown files
            otherFileDetails.ForEach(x =>
            {
                if (x.HitType == HitTypeEnum.Unknown && Model.Config.SelectedFixHitTypes.Contains(HitTypeEnum.Unknown) ||
                    x.HitType == HitTypeEnum.Unsupported && Model.Config.SelectedFixHitTypes.Contains(HitTypeEnum.Unsupported))
                {
                    x.Deleted = true;
                    TableUtils.Delete(x.Path, x.HitType, null);
                }
            });

            // delete empty backup folders - i.e. if there are no files (empty sub-directories are allowed)
            if (Directory.Exists(_activeBackupFolder))
            {
                var files = Directory.EnumerateFiles(_activeBackupFolder, "*", SearchOption.AllDirectories);
                if (!files.Any())
                {
                    Logger.Info($"Deleting empty backup folder: '{_activeBackupFolder}'");
                    Directory.Delete(_activeBackupFolder, true);
                }
            }

            return fixedFileDetails;
        }

        private static void AddMissingStatus(List<Game> games)
        {
            games.ForEach(game =>
            {
                // add missing content
                game.Content.ContentHitsCollection.ForEach(contentHitCollection =>
                {
                    if (!contentHitCollection.Hits.Any(hit => hit.Type == HitTypeEnum.Valid || hit.Type == HitTypeEnum.WrongCase))
                        contentHitCollection.Add(HitTypeEnum.Missing, game.Description);
                });
            });
        }

        private static string _activeBackupFolder;
    }
}