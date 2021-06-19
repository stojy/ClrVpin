using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClrVpin.Logging;
using ClrVpin.Models;
using ClrVpin.Shared;
using Utils;

namespace ClrVpin.Rebuilder
{
    public static class RebuilderUtils
    {
        public static List<FixFileDetail> Check(List<Game> games)
        {
            var otherFiles = new List<FixFileDetail>();

            // determine the destination type
            // - todo; scan non-media content, e.g. tables and b2s
            var contentType = Model.Config.GetFrontendFolders().First(x => x.Description == Model.Config.DestinationContentType);

            // for the specified content type, match files (from the source folder) with the correct file extension(s) to a table
            var mediaFiles = TableUtils.GetMediaFileNames(contentType, Model.Config.SourceFolder);
            var unknownMedia = TableUtils.AssociateMediaFilesWithGames(games, mediaFiles, contentType.Enum, game => game.Content.ContentHitsCollection.First(contentHits => contentHits.Type == contentType.Enum));
            otherFiles.AddRange(unknownMedia);

            // identify any unsupported files, i.e. files in the directory that don't have a matching extension
            var unsupportedFiles = TableUtils.GetUnsupportedMediaFileDetails(contentType, Model.Config.SourceFolder);
            otherFiles.AddRange(unsupportedFiles);

            return otherFiles;
        }

        public static async Task<List<FixFileDetail>> MergeAsync(List<Game> games, List<FixFileDetail> otherFileDetails, string backupFolder)
        {
            var mergedFileDetails = await Task.Run(() => Merge(games, otherFileDetails, backupFolder));
            return mergedFileDetails;
        }

        private static List<FixFileDetail> Merge(List<Game> games, List<FixFileDetail> otherFileDetails, string backupFolder)
        {
            _activeBackupFolder = TableUtils.GetActiveBackupFolder(backupFolder);

            var deletedFiles = new List<FixFileDetail>();
            var matchedMergedFiles = new List<FixFileDetail>();
            var matchedUnusedFiles = new List<FixFileDetail>();

            // merge files associated with games, if they satisfy the merge criteria
            games.ForEach(game =>
            {
                // determine the destination content type and relevant hit collection from the games collection
                var contentType = Model.Config.GetFrontendFolders().First(x => x.Description == Model.Config.DestinationContentType);
                var contentHitCollection = game.Content.ContentHitsCollection.First(x => x.Type == contentType.Enum);
                
                if (TableUtils.TryGet(contentHitCollection.Hits, out var hit, HitTypeEnum.Valid))
                {
                    // valid hit exists.. so copy file and delete other hits, i.e. other hits aren't as relevant
                    matchedMergedFiles.Add(Merge(hit, game));
                    matchedUnusedFiles.AddRange(TableUtils.DeleteAllExcept(contentHitCollection.Hits, hit, Model.Config.SelectedMatchTypes));
                }
                else if (TableUtils.TryGet(contentHitCollection.Hits, out hit, HitTypeEnum.WrongCase, HitTypeEnum.TableName, HitTypeEnum.Fuzzy))
                {
                    // for all 3 hit types.. rename file and delete other entries
                    deletedFiles.Add(TableUtils.Rename(hit, game, Model.Config.SelectedMatchTypes));
                    matchedUnusedFiles.AddRange(TableUtils.DeleteAllExcept(contentHitCollection.Hits, hit, Model.Config.SelectedMatchTypes));
                }

                // other hit types are n/a
                // - duplicate extension - already taken care as a valid hit will exist
                // - unknown - not associated with a game.. handled elsewhere
                // - missing - can't be fixed.. requires file to be downloaded
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

            return matchedUnusedFiles;
        }

        private static FixFileDetail Merge(Hit hit, Game game)
        {
            //var matched = new FixFileDetail(hit.ContentTypeEnum, hit.Type, false, false, hit.Path, hit.Size ?? 0);
            //matchedMergedFiles.Add(matched);

            var renamed = false;

            if (Model.Config.SelectedFixHitTypes.Contains(hit.Type))
            {
                renamed = true;

                var extension = Path.GetExtension(hit.Path);
                var path = Path.GetDirectoryName(hit.Path);
                var newFile = Path.Combine(path!, $"{game.Description}{extension}");

                var backupFileName = TableUtils.CreateBackupFileName(hit.Path);
                var prefix = Model.Config.TrainerWheels ? "Skipped (trainer wheels are on) " : "";
                Logger.Info($"{prefix}Renaming file.. type: {hit.Type.GetDescription()}, content: {hit.ContentType}, original: {hit.Path}, new: {newFile}, backup: {backupFileName}");

                if (!Model.Config.TrainerWheels)
                {
                    File.Copy(hit.Path!, backupFileName, true);
                    File.Move(hit.Path!, newFile, true);
                }
            }

            return new FixFileDetail(hit.ContentTypeEnum, hit.Type, false, renamed, hit.Path, hit.Size ?? 0);
        }

        private static string _activeBackupFolder;
    }
}