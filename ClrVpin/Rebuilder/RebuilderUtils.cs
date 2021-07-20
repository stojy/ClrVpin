using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClrVpin.Logging;
using ClrVpin.Models;
using ClrVpin.Models.Rebuilder;
using ClrVpin.Models.Settings;
using ClrVpin.Shared;
using Utils;

namespace ClrVpin.Rebuilder
{
    public static class RebuilderUtils
    {
        static RebuilderUtils()
        {
            _settings = SettingsManager.Settings;
        }

        public static List<FileDetail> Check(List<Game> games)
        {
            // determine the destination type
            // - todo; scan non-media content, e.g. tables and b2s
            var contentType = Model.Config.GetDestinationContentType();

            // for the specified content type, match files (from the source folder) with the correct file extension(s) to a table
            var mediaFiles = TableUtils.GetMediaFileNames(contentType, _settings.Rebuilder.SourceFolder);
            var unmatchedFiles = TableUtils.AssociateMediaFilesWithGames(games, mediaFiles, contentType.Enum,
                game => game.Content.ContentHitsCollection.First(contentHits => contentHits.Type == contentType.Enum));

            // identify any unsupported files, i.e. files in the directory that don't have a matching extension
            var unsupportedFiles = TableUtils.GetUnsupportedMediaFileDetails(contentType, _settings.Rebuilder.SourceFolder);

            return unmatchedFiles.Concat(unsupportedFiles).ToList();
        }

        public static async Task<List<FileDetail>> MergeAsync(List<Game> games, string backupFolder)
        {
            var mergedFileDetails = await Task.Run(() => Merge(games, backupFolder));
            return mergedFileDetails;
        }

        private static List<FileDetail> Merge(IEnumerable<Game> games, string backupFolder)
        {
            TableUtils.SetActiveBackupFolder(backupFolder);

            // filter games to only those that have hits for the destination content type
            var contentType = Model.Config.GetDestinationContentType();
            var gamesWithContent = games.Where(g => g.Content.ContentHitsCollection.Any(x => x.Type == contentType.Enum && x.Hits.Any()));

            // EVERY GAME THAT HAS A HIT (IRRESPECTIVE OF MATCH CRITERIA) WILL HAVE A GAME FILE RETURNED, i.e. irrespective of whether..
            // - match criteria is selected or relevant
            // - skip options are selected or relevant
            var gameFiles = new List<FileDetail>();
            gamesWithContent.ForEach(game =>
            {
                // retrieve the relevant content hit collection
                var contentHitCollection = game.Content.ContentHitsCollection.First(x => x.Hits.Any());

                // merge ALL of the selected hit types
                // - for each supported file there, there will be 1 hit type
                // - if their are multiple hit type matches.. then a subsequent 'scanner' (aka clean) run will be required to clean up the extra files
                var mergeableHits = contentHitCollection.Hits.Where(hit => hit.Type.In(Config.FixablePrioritizedHitTypeEnums));

                // merge each hit
                mergeableHits.ForEach(hit => gameFiles.Add(Merge(hit, game, _settings.Rebuilder.SelectedMatchTypes)));
            });

            // delete empty backup folders - i.e. if there are no files (empty sub-directories are allowed)
            TableUtils.DeleteActiveBackupFolderIfEmpty();

            return gameFiles;
        }

        // ReSharper disable once UnusedParameter.Local
        private static FileDetail Merge(Hit hit, Game _, ICollection<HitTypeEnum> supportedHitTypes)
        {
            var ignore = false;
            var sourceFileInfo = hit.FileInfo; // file to be copied, i.e. into the VP folder (potentially overriding)

            // construct the destination file name - i.e. the location the source file will be copied to
            var contentType = Model.Config.GetDestinationContentType();
            var destinationFileName = Path.Combine(contentType.Folder, hit.File);
            var destinationFileInfo = File.Exists(destinationFileName) ? new FileInfo(destinationFileName) : null;

            // ignore file from either..
            // - hit type NOT selected OR
            // - ignore option selected
            if (supportedHitTypes.Contains(hit.Type))
            {
                ignore = ShouldIgnore(hit, sourceFileInfo, destinationFileInfo);
                if (ignore)
                {
                    if (_settings.TrainerWheels)
                        Log("Ignored merging", "trainer wheels", hit, sourceFileInfo, destinationFileInfo, destinationFileName);
                    else
                    {
                        Log("Merging file", null, hit, sourceFileInfo, destinationFileInfo, destinationFileName);

                        if (!_settings.TrainerWheels)
                        {
                            if (File.Exists(destinationFileName))
                                TableUtils.Backup(destinationFileName, "deleted");

                            TableUtils.Backup(sourceFileInfo.Name, "merged");

                            // todo; preserve timestamp
                            // todo; copy vs move.. i.e. delete source file
                            File.Move(sourceFileInfo.Name, destinationFileName, true);
                        }
                    }
                }
            }

            return new FileDetail(hit.ContentTypeEnum, hit.Type, ignore ? FixFileTypeEnum.Merged : null, sourceFileInfo.Name, hit.Size ?? 0);
        }

        private static bool ShouldIgnore(Hit hit, FileInfo sourceFileInfo, FileInfo destinationFileInfo)
        {
            if (destinationFileInfo != null)
            {
                if (_settings.Rebuilder.SelectedIgnoreOptions.Contains(IgnoreOptionEnum.IgnoreSmaller) && sourceFileInfo.Length < destinationFileInfo.Length * 0.5)
                    return SkipMerge(IgnoreOptionEnum.IgnoreSmaller, hit, sourceFileInfo, destinationFileInfo);
                if (_settings.Rebuilder.SelectedIgnoreOptions.Contains(IgnoreOptionEnum.IgnoreOlder) && sourceFileInfo.LastWriteTime < destinationFileInfo.LastWriteTime)
                    return SkipMerge(IgnoreOptionEnum.IgnoreOlder, hit, sourceFileInfo, destinationFileInfo);
            }

            // if the file doesn't exist then there's no reason to not merge it
            return true;
        }

        private static bool SkipMerge(IgnoreOptionEnum optionEnum, Hit hit, FileInfo sourceFileInfo, FileInfo destinationFileInfo)
        {
            Log("Ignored merging", optionEnum.GetDescription(), hit, sourceFileInfo, destinationFileInfo);
            return false;
        }

        private static void Log(string prefix, string optionDescription, Hit hit, FileInfo sourceFileInfo, FileInfo destinationFileInfo, string destinationFileName = null)
        {
            // files..
            // 1. source file - will always exist since this is the new file to be merged
            // 2. destination file - may not exist, i.e. this is a new file name (aka new content)

            var optionDetail = optionDescription == null ? "" : $"option: '{optionDescription}', ";
            var destinationLengthDetail = destinationFileInfo == null ? "NEW" : destinationFileInfo.Length.ToString();

            Logger.Info($"{prefix} - {optionDetail}'type: {hit.Type.GetDescription()}, content: {hit.ContentType}, " +
                        $"source: {sourceFileInfo.Name} ({sourceFileInfo.Length}), destination: {destinationFileInfo?.Name ?? destinationFileName} ({destinationLengthDetail})");
        }

        private static readonly Models.Settings.Settings _settings;
    }
}