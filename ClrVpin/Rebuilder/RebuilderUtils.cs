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
            _settings = Model.Settings;
        }

        public static List<FileDetail> Check(List<Game> games)
        {
            // determine the destination type
            var contentType = _settings.GetSelectedDestinationContentType();

            // for the specified content type, match files (from the source folder) with the correct file extension(s) to a table
            var mediaFiles = TableUtils.GetContentFileNames(contentType, _settings.Rebuilder.SourceFolder);
            var unmatchedFiles = TableUtils.AssociateContentFilesWithGames(games, mediaFiles, contentType,
                game => game.Content.ContentHitsCollection.First(contentHits => contentHits.Enum == contentType.Enum));

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
            FileUtils.SetActiveBackupFolder(backupFolder);

            // filter games to only those that have hits for the destination content type
            var contentType = _settings.GetSelectedDestinationContentType();
            var gamesWithContent = games.Where(g => g.Content.ContentHitsCollection.Any(x => x.Enum == contentType.Enum && x.Hits.Any()));

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
                var mergeableHits = contentHitCollection.Hits.Where(hit => hit.Type.In(StaticSettings.FixablePrioritizedHitTypeEnums));

                // merge each hit
                mergeableHits.ForEach(hit => gameFiles.Add(Merge(hit, game, _settings.Rebuilder.SelectedMatchTypes)));
            });

            // delete empty backup folders - i.e. if there are no files (empty sub-directories are allowed)
            FileUtils.DeleteActiveBackupFolderIfEmpty();

            return gameFiles;
        }

        // ReSharper disable once UnusedParameter.Local
        private static FileDetail Merge(Hit hit, Game game, ICollection<HitTypeEnum> supportedHitTypes)
        {
            var ignore = false;
            var sourceFileInfo = hit.FileInfo; // file to be copied, i.e. into the VP folder (potentially overriding)

            // construct the destination file name - i.e. the location the source file will be copied to
            var contentType = _settings.GetSelectedDestinationContentType();
            var destinationFileName = Path.Combine(contentType.Folder, hit.File);
            var destinationFileInfo = File.Exists(destinationFileName) ? new FileInfo(destinationFileName) : null;

            // ignore file from either..
            // - hit type NOT selected OR
            // - ignore option selected
            if (supportedHitTypes.Contains(hit.Type))
            {
                ignore = ShouldIgnore(game, hit, sourceFileInfo, destinationFileInfo);
                if (!ignore)
                {
                    var shouldDeleteSource = MergeOptionEnum.RemoveSource.In(Model.Settings.Rebuilder.SelectedMergeOptions);
                    var preserveDateModified = MergeOptionEnum.PreserveDateModified.In(Model.Settings.Rebuilder.SelectedMergeOptions);

                    Logger.Info($"Merging.. table: {game.GetContentName(_settings.GetContentType(hit.ContentTypeEnum).Category)}, type: {hit.Type.GetDescription()}, content: {hit.ContentType}");
                    FileUtils.Merge(hit.Path, destinationFileName, hit.Type, hit.ContentType, shouldDeleteSource, preserveDateModified, contentType.KindredExtensionsList, backupFile => hit.Path = backupFile);
                }
            }

            return new FileDetail(hit.ContentTypeEnum, hit.Type, ignore ? FixFileTypeEnum.Merged : null, sourceFileInfo.Name, hit.Size ?? 0);
        }

        private static bool ShouldIgnore(Game game, Hit hit, FileInfo sourceFileInfo, FileInfo destinationFileInfo)
        {
            if (destinationFileInfo != null)
            {
                if (_settings.Rebuilder.SelectedIgnoreOptions.Contains(IgnoreOptionEnum.IgnoreSmaller) && sourceFileInfo.Length < destinationFileInfo.Length * 0.5)
                    return ProcessIgnore(game, IgnoreOptionEnum.IgnoreSmaller, hit, sourceFileInfo, destinationFileInfo);
                if (_settings.Rebuilder.SelectedIgnoreOptions.Contains(IgnoreOptionEnum.IgnoreOlder) && sourceFileInfo.LastWriteTime < destinationFileInfo.LastWriteTime)
                    return ProcessIgnore(game, IgnoreOptionEnum.IgnoreOlder, hit, sourceFileInfo, destinationFileInfo);
            }

            // if the file doesn't exist then there's no reason to not merge it
            return false;
        }

        private static bool ProcessIgnore(Game game, IgnoreOptionEnum optionEnum, Hit hit, FileSystemInfo sourceFileInfo, FileSystemInfo destinationFileInfo)
        {
            var prefix = _settings.Rebuilder.DeleteIgnoredFiles ? "Removing (delete ignored selected)" : "Skipping (ignore option selected)";
            Logger.Info($"{prefix}.. table: {game.GetContentName(_settings.GetContentType(hit.ContentTypeEnum).Category)}, type: {hit.Type.GetDescription()}, " +
                        $"content: {hit.ContentType}, ignore option: {optionEnum.GetDescription()}, delete ignored: {_settings.Rebuilder.DeleteIgnoredFiles}");

            if (_settings.Rebuilder.DeleteIgnoredFiles)
            {
                FileUtils.Delete(hit.Path, hit.Type, hit.ContentType, newFile => hit.Path = newFile);
                Logger.Debug($"  dst: {FileUtils.GetFileInfoStatistics(destinationFileInfo.FullName)}");
            }
            else
            {
                // files..
                // 1. source file - will always exist since this is the new file to be merged
                // 2. destination file - may not exist, i.e. this is a new file name (aka new content)
                Logger.Debug($"- ignored..\n  src: {FileUtils.GetFileInfoStatistics(sourceFileInfo.FullName)}\n  dst: {FileUtils.GetFileInfoStatistics(destinationFileInfo.FullName)}");
            }

            return true;
        }

        private static readonly Models.Settings.Settings _settings;
    }
}