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
            var contentType = Config.GetDestinationContentType();

            // for the specified content type, match files (from the source folder) with the correct file extension(s) to a table
            var mediaFiles = TableUtils.GetMediaFileNames(contentType, Model.Config.SourceFolder);
            var unknownMedia = TableUtils.AssociateMediaFilesWithGames(games, mediaFiles, contentType.Enum, game => game.Content.ContentHitsCollection.First(contentHits => contentHits.Type == contentType.Enum));
            otherFiles.AddRange(unknownMedia);

            // identify any unsupported files, i.e. files in the directory that don't have a matching extension
            var unsupportedFiles = TableUtils.GetUnsupportedMediaFileDetails(contentType, Model.Config.SourceFolder);
            otherFiles.AddRange(unsupportedFiles);

            return otherFiles;
        }

        public static async Task<List<FixFileDetail>> MergeAsync(List<Game> games, string backupFolder)
        {
            var mergedFileDetails = await Task.Run(() => Merge(games, backupFolder));
            return mergedFileDetails;
        }

        private static List<FixFileDetail> Merge(IEnumerable<Game> games, string backupFolder)
        {
            _activeBackupFolder = TableUtils.GetActiveBackupFolder(backupFolder);

            // filter games to only those that have hits for the destination content type
            // - the hits are for ALL criteria, i.e. irrespective of whether they are selected or not!
            var contentType = Config.GetDestinationContentType();
            var gamesWithContent = games.Where(g => g.Content.ContentHitsCollection.Any(x => x.Type == contentType.Enum && x.Hits.Any()));

            // merge files associated with games, if they satisfy the merge criteria
            var mergedFileDetails = new List<FixFileDetail>();
            gamesWithContent.ForEach(game =>
            {
                // retrieve the relevant content hit collection
                var contentHitCollection = game.Content.ContentHitsCollection.First(x => x.Hits.Any());
                
                // merge ALL of the selected hit types
                // - if their are multiple hit type matches.. then a subsequent 'scanner' (aka clean) run will be required
                var mergeableHits = contentHitCollection.Hits.Where(hit => hit.Type.In(Config.FixablePrioritizedHitTypeEnums));

                // merge each hit
                mergeableHits.ForEach(hit => mergedFileDetails.Add(Merge(hit, game, Model.Config.SelectedMatchTypes)));
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

            return mergedFileDetails;
        }

        private static FixFileDetail Merge(Hit hit, Game game, ICollection<HitTypeEnum> supportedHitTypes)
        {
            var canMerge = false;
            var sourceFileName = hit.Path;

            // construct the destination file name - i.e. the location the source file will be copied to
            var contentType = Config.GetDestinationContentType();
            var destinationFileName = Path.Combine(contentType.Folder, hit.File);

            if (supportedHitTypes.Contains(hit.Type))
            {
                canMerge = CanMerge(hit.Type.GetDescription(), sourceFileName, destinationFileName);
                if (canMerge)
                {
                    var prefix = Model.Config.TrainerWheels ? "Skipped (trainer wheels are on) " : "";
                    Logger.Info($"{prefix}Merging file.. type: {hit.Type.GetDescription()}, content: {hit.ContentType}, source: {sourceFileName}, destination: {destinationFileName}");

                    if (!Model.Config.TrainerWheels)
                    {
                        if (File.Exists(destinationFileName))
                            TableUtils.Backup(destinationFileName, "deleted");

                        TableUtils.Backup(sourceFileName, "merged");

                        // todo; preserve timestamp
                        // todo; copy vs move.. i.e. delete source file
                        File.Move(sourceFileName, destinationFileName, true);
                    }
                }
            }

            return new FixFileDetail(hit.ContentTypeEnum, hit.Type, canMerge ? FixFileTypeEnum.Merged : null, sourceFileName, hit.Size ?? 0);
        }

        private static bool CanMerge(string hitTypeDescription, string sourceFileName, string destinationFileName)
        {
            if (File.Exists(destinationFileName))
            {
                var sourceFileInfo = new FileInfo(sourceFileName);
                var destinationFileInfo = new FileInfo(destinationFileName);

                if (Model.Config.SelectedMergeOptions.Contains(MergeOptionEnum.IgnoreSmaller) && sourceFileInfo.Length * 0.5 < destinationFileInfo.Length)
                    return SkipMerge(MergeOptionEnum.IgnoreSmaller, hitTypeDescription, sourceFileInfo, destinationFileInfo);
                if (Model.Config.SelectedMergeOptions.Contains(MergeOptionEnum.IgnoreOlder) && sourceFileInfo.LastWriteTime < destinationFileInfo.LastWriteTime)
                    return SkipMerge(MergeOptionEnum.IgnoreOlder, hitTypeDescription, sourceFileInfo, destinationFileInfo);
            }

            // if the file doesn't exist
            return true;
        }

        private static bool SkipMerge(MergeOptionEnum mergeOption, string hitTypeDescription, FileInfo sourceFileInfo, FileInfo destinationFileInfo)
        {
            Logger.Info(
                $"Skipped merging - option: '{mergeOption.GetDescription()}', type: {hitTypeDescription}, existing: {destinationFileInfo.FullName} ({destinationFileInfo.Length}), source: {sourceFileInfo.FullName} ({sourceFileInfo.Length})");
            return false;
        }

        private static string _activeBackupFolder;
    }
}