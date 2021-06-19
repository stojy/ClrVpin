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
                
                if (TryGet(contentHitCollection.Hits, out var hit, HitTypeEnum.Valid))
                {

                    // valid hit exists.. so copy file and delete other hits, i.e. other hits aren't as relevant
                    matchedMergedFiles.Add(Merge(hit, game));
                    matchedUnusedFiles.AddRange(DeleteAllExcept(contentHitCollection.Hits, hit));
                }
                else if (TryGet(contentHitCollection.Hits, out hit, HitTypeEnum.WrongCase, HitTypeEnum.TableName, HitTypeEnum.Fuzzy))
                {
                    // for all 3 hit types.. rename file and delete other entries
                    deletedFiles.Add(Rename(hit, game));
                    matchedUnusedFiles.AddRange(DeleteAllExcept(contentHitCollection.Hits, hit));
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
                    Delete(x.Path, x.HitType, null);
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

        private static bool TryGet(IEnumerable<Hit> hits, out Hit hit, params HitTypeEnum[] hitTypes)
        {
            // return the first entry found
            hit = hits.FirstOrDefault(h => hitTypes.Contains(h.Type));
            return hit != null;
        }

        private static IEnumerable<FixFileDetail> DeleteAllExcept(IEnumerable<Hit> hits, Hit hit)
        {
            var deleted = new List<FixFileDetail>();

            // delete all 'real' files except the specified hit
            hits.Except(hit).Where(x => x.Size.HasValue).ForEach(h => deleted.Add(Delete(h)));

            return deleted;
        }

        private static FixFileDetail Delete(Hit hit)
        {
            var deleted = false;

            // only delete file if configured to do so
            if (Model.Config.SelectedFixHitTypes.Contains(hit.Type))
            {
                deleted = true;
                Delete(hit.Path, hit.Type, hit.ContentType);
            }

            return new FixFileDetail(hit.ContentTypeEnum, hit.Type, deleted, false, hit.Path, hit.Size ?? 0);
        }

        private static void Delete(string file, HitTypeEnum hitType, string contentType)
        {
            var backupFileName = CreateBackupFileName(file);

            var prefix = Model.Config.TrainerWheels ? "Skipped (trainer wheels are on) " : "";
            Logger.Warn($"{prefix}Deleting file.. type: {hitType.GetDescription()}, content: {contentType ?? "n/a"}, file: {file}, backup: {backupFileName}");

            if (!Model.Config.TrainerWheels)
                File.Move(file, backupFileName, true);
        }

        private static FixFileDetail Rename(Hit hit, Game game)
        {
            var renamed = false;

            if (Model.Config.SelectedFixHitTypes.Contains(hit.Type))
            {
                renamed = true;

                var extension = Path.GetExtension(hit.Path);
                var path = Path.GetDirectoryName(hit.Path);
                var newFile = Path.Combine(path!, $"{game.Description}{extension}");

                var backupFileName = CreateBackupFileName(hit.Path);
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

                var backupFileName = CreateBackupFileName(hit.Path);
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

        private static string CreateBackupFileName(string file)
        {
            var baseFolder = Path.GetDirectoryName(file)!.Split("\\").Last();
            var folder = Path.Combine(_activeBackupFolder, baseFolder);
            var destFileName = Path.Combine(folder, Path.GetFileName(file));
            
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            
            return destFileName;
        }

        private static string _activeBackupFolder;
    }
}