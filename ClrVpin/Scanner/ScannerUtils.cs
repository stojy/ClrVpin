using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClrVpin.Logging;
using ClrVpin.Models;
using ClrVpin.Shared;
using Utils;

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
                var unknownMedia = TableUtils.AssociateMediaFilesToGames(games, mediaFiles, contentType.Enum, game => game.Content.ContentHitsCollection.First(contentHits => contentHits.Type == contentType.Enum));
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
                    if (TryGet(contentHitCollection.Hits, out var hit, HitTypeEnum.Valid))
                    {
                        // valid hit exists.. so delete everything else
                        fixedFileDetails.AddRange(DeleteAllExcept(contentHitCollection.Hits, hit));
                    }
                    else if (TryGet(contentHitCollection.Hits, out hit, HitTypeEnum.WrongCase, HitTypeEnum.TableName, HitTypeEnum.Fuzzy))
                    {
                        // for all 3 hit types.. rename file and delete other entries
                        fixedFileDetails.Add(Rename(hit, game));
                        fixedFileDetails.AddRange(DeleteAllExcept(contentHitCollection.Hits, hit));
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

            return fixedFileDetails;
        }

        private static bool TryGet(IEnumerable<Hit> hits, out Hit hit, params HitTypeEnum[] hitTypes)
        {
            // return the first entry found
            hit = hits.FirstOrDefault(h => hitTypes.Contains(h.Type));
            return hit != null;
        }

        // todo.. move/reference TableUtils or similar?
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

        private static string CreateBackupFileName(string file)
        {
            var baseFolder = Path.GetDirectoryName(file)!.Split("\\").Last();
            var folder = Path.Combine(_activeBackupFolder, baseFolder);
            var destFileName = Path.Combine(folder, Path.GetFileName(file));
            
            // store backup file in the same folder structure as the source file
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            
            return destFileName;
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