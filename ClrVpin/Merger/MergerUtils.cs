using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClrVpin.Logging;
using ClrVpin.Models.Merger;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Database;
using ClrVpin.Models.Shared.Enums;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using ClrVpin.Shared.Fuzzy;
using ClrVpin.Shared.Utils;
using Utils.Extensions;

namespace ClrVpin.Merger
{
    public static class MergerUtils
    {
        static MergerUtils()
        {
            _settings = Model.Settings;
        }

        public static async Task<List<FileDetail>> CheckAndMatchAsync(List<LocalGame> games, Action<string, int, int> updateProgress)
        {
            var unmatchedFiles = await Task.Run(() => CheckAndMatch(games, updateProgress));
            return unmatchedFiles;
        }

        public static async Task<List<FileDetail>> MergeAsync(List<LocalGame> games, string backupFolder, Action<string, int, int> updateProgress)
        {
            var mergedFileDetails = await Task.Run(() => Merge(games, backupFolder, updateProgress));
            return mergedFileDetails;
        }

        public static async Task RemoveUnmatchedIgnoredAsync(List<FileDetail> unmatchedFiles, Action<string, int, int> updateProgress)
        {
            await Task.Run(() => RemoveUnmatchedIgnored(unmatchedFiles, updateProgress));
        }

        private static List<FileDetail> CheckAndMatch(IList<LocalGame> games, Action<string, int, int> updateProgress)
        {
            // determine the destination type
            var contentType = _settings.GetSelectedDestinationContentType();

            // for the specified content type, match files (from the source folder) with the correct file extension(s) to a table
            var contentFiles = ContentUtils.GetContentFileNames(contentType, _settings.Merger.SourceFolder);
            var unmatchedSupportedFiles = ContentUtils.MatchFilesToLocal(games, contentFiles, contentType, 
                game => game.Content.ContentHitsCollection.First(contentHits => contentHits.Enum == contentType.Enum),
                (fileName, fileCount, _) => updateProgress(fileName, fileCount, contentFiles.Count));

            // identify any unsupported files, i.e. files in the directory that don't have a matching extension
            var nonContentFiles = ContentUtils.GetNonContentFileDetails(contentType, _settings.Merger.SourceFolder);

            // unmatchedFiles = unmatchedContentFiles + nonContentFiles
            return unmatchedSupportedFiles.Concat(nonContentFiles).ToList();
        }

        private static List<FileDetail> Merge(IEnumerable<LocalGame> games, string backupFolder, Action<string, int, int> updateProgress)
        {
            FileUtils.SetActiveBackupFolder(backupFolder);

            // filter games to only those that have hits for the destination content type
            var contentType = _settings.GetSelectedDestinationContentType();
            var gamesWithContent = games.Where(g => g.Content.ContentHitsCollection.Any(x => x.Enum == contentType.Enum && x.Hits.Any())).ToList();

            // EVERY GAME THAT HAS A HIT (IRRESPECTIVE OF MATCH CRITERIA) WILL HAVE A GAME FILE RETURNED, i.e. irrespective of whether..
            // - match criteria is selected or relevant
            // - ignore criteria is selected or relevant
            var gameFiles = new List<FileDetail>();
            
            gamesWithContent.ForEachParallel((game, i) =>
            {
                updateProgress(game.Game.Description, i + 1, gamesWithContent.Count);

                // retrieve the relevant content hit collection
                var contentHitCollection = game.Content.ContentHitsCollection.First(x => x.Hits.Any());

                // merge ALL of the selected hit types
                // - for each supported file that exists, there will be 1 hit type
                // - if their are multiple hit type matches.. then a subsequent 'cleaner' run will be required to clean up the extra files
                var mergeableHits = contentHitCollection.Hits.Where(hit => hit.Type.In(StaticSettings.FixablePrioritizedHitTypeEnums));

                // merge each hit
                mergeableHits.ForEach(hit => gameFiles.Add(Merge(hit, game, _settings.Merger.SelectedMatchTypes)));
            });

            return gameFiles;
        }

        private static FileDetail Merge(Hit hit, LocalGame localGame, ICollection<HitTypeEnum> supportedHitTypes)
        {
            var fixFileType = FixFileTypeEnum.Skipped;
            var sourceFileInfo = hit.FileInfo; // file to be copied, i.e. into the VP folder (potentially overriding)

            // construct the destination file name - i.e. the location the source file will be copied to
            var contentType = _settings.GetSelectedDestinationContentType();
            var destinationFileName = Path.Combine(contentType.Folder, hit.File);
            var destinationFileInfo = File.Exists(destinationFileName) ? new FileInfo(destinationFileName) : null;

            // construct the correct destination file name - i.e. the file name that WILL be used when the cleaner is eventually run (typically after the merge)
            // - calculated here the purpose of logging, i.e. so that the file details of the file that will potentially be overwritten (when scanning is run) is displayed
            // - selecting from the relevant hit.Type would likely be more efficient, but not done because full file paths are required/useful for logging
            var correctDestinationFileName = FileUtils.GetCorrectFile(localGame, contentType.Category, contentType.Folder, hit.Extension);
            var correctDestinationFileInfo = File.Exists(correctDestinationFileName) ? new FileInfo(correctDestinationFileName) : null;

            void LogFuzzyMatch()
            {
                // log to identify fuzzy matches - anything that isn't an exact match
                if (Path.GetFileName(destinationFileName) != Path.GetFileName(correctDestinationFileName))
                {
                    var (description, warning) = Fuzzy.GetScoreDetail(hit.Score);
                    var message = $"- fuzzy match (score: {description}).." +
                                  $"\n  source: {FileUtils.GetFileInfoStatistics(hit.Path)}\n  match:  {FileUtils.GetFileInfoStatistics(correctDestinationFileName)}";
                    if (warning)
                        Logger.Warn(message);
                    else
                        Logger.Debug(message);
                }
            }

            // ignore file from either..
            // - hit type NOT selected OR
            // - ignore option selected
            if (supportedHitTypes.Contains(hit.Type))
            {
                fixFileType = FixFileTypeEnum.Ignored;

                if (!ShouldIgnore(localGame.Game, hit, sourceFileInfo, destinationFileInfo ?? correctDestinationFileInfo, LogFuzzyMatch))
                {
                    fixFileType = FixFileTypeEnum.Merged;

                    var shouldDeleteSource = MergeOptionEnum.RemoveSource.In(Model.Settings.Merger.SelectedMergeOptions);
                    var preserveDateModified = MergeOptionEnum.PreserveDateModified.In(Model.Settings.Merger.SelectedMergeOptions);

                    Logger.Info($"Merging.. table: {localGame.Game.Name}, description: {localGame.Game.Description}, type: {hit.Type.GetDescription()}, content: {hit.ContentType}", isHighlight: true);
                    LogFuzzyMatch();
                    FileUtils.Merge(hit.Path, destinationFileName, hit.Type, hit.ContentType, shouldDeleteSource, preserveDateModified, contentType.KindredExtensionsList, backupFile => hit.Path = backupFile);
                }
            }

            return new FileDetail(hit.ContentTypeEnum, hit.Type, fixFileType, sourceFileInfo.Name, hit.Size ?? 0);
        }

        private static bool ShouldIgnore(Game game, Hit hit, FileInfo sourceFileInfo, FileInfo destinationFileInfo, Action logAction)
        {
            // opt out: scan through each ignore criteria to determine if the file should be considered 'merge worthy'
            // - unlike cleaner 'multiple match preference'.. which is more of an 'opt in'

            // contains words - destination file isn't required (although a table match is required)
            if (_settings.Merger.SelectedIgnoreCriteria.Contains(IgnoreCriteriaEnum.IgnoreIfContainsWords) && _settings.Merger.IgnoreIWords.Any(x => sourceFileInfo.Name.ToLower().Contains(x)))
                return ProcessIgnore(game, IgnoreCriteriaEnum.IgnoreIfContainsWords.GetDescription(), hit, sourceFileInfo, destinationFileInfo, logAction);

            // source vs destination file
            if (destinationFileInfo != null)
            {
                var thresholdSizePercentage = _settings.Merger.IgnoreIfSmallerPercentage / 100;
                var actualSizePercentage = (decimal)sourceFileInfo.Length / destinationFileInfo.Length;
                if (_settings.Merger.SelectedIgnoreCriteria.Contains(IgnoreCriteriaEnum.IgnoreIfSmaller) && actualSizePercentage <= thresholdSizePercentage)
                    return ProcessIgnore(game, $"{IgnoreCriteriaEnum.IgnoreIfSmaller.GetDescription()} (threshold: {thresholdSizePercentage:P2}, actual:{actualSizePercentage:P2}", hit, sourceFileInfo, destinationFileInfo, logAction);

                if (_settings.Merger.SelectedIgnoreCriteria.Contains(IgnoreCriteriaEnum.IgnoreIfNotNewer) && sourceFileInfo.LastWriteTime <= destinationFileInfo.LastWriteTime)
                    return ProcessIgnore(game, IgnoreCriteriaEnum.IgnoreIfNotNewer.GetDescription(), hit, sourceFileInfo, destinationFileInfo, logAction);
            }

            // if the file doesn't exist then there's no reason to not merge it
            return false;
        }

        private static bool ProcessIgnore(Game game, string ignoreCriteriaDescription, Hit hit, FileSystemInfo sourceFileInfo, FileSystemInfo destinationFileInfo, Action logAction) => ProcessIgnore(
            game, ignoreCriteriaDescription, hit.Type, hit.ContentTypeEnum, hit, sourceFileInfo, destinationFileInfo, logAction);

        private static bool ProcessIgnore(Game game, string ignoreCriteriaDescription, HitTypeEnum hitTypeEnum, ContentTypeEnum contentTypeEnum, Hit hit, FileSystemInfo sourceFileInfo,
            FileSystemInfo destinationFileInfo, Action logAction = null)
        {
            var prefix = _settings.Merger.DeleteIgnoredFiles ? "Removing (delete ignored selected)" : "Skipping (ignore option selected)";
            Logger.Info($"{prefix}.. table: {game?.Name ?? "n/a"}, description: {game?.Description ?? "n/a"}, type: {hitTypeEnum.GetDescription()}, " +
                        $"content: {contentTypeEnum.GetDescription()}, ignore option: {ignoreCriteriaDescription}, delete ignored: {_settings.Merger.DeleteIgnoredFiles}");
            logAction?.Invoke();

            if (_settings.Merger.DeleteIgnoredFiles)
            {
                FileUtils.DeleteIgnored(sourceFileInfo.FullName, destinationFileInfo?.FullName, hitTypeEnum, contentTypeEnum.GetDescription(), newFile =>
                {
                    if (hit != null)
                        hit.Path = newFile;
                });
            }
            else
            {
                // files..
                // 1. source file - will always exist since this is the new file to be merged
                // 2. destination file - may not exist, i.e. this is a new file name (aka new content)
                Logger.Debug($"- ignored..\n  source: {FileUtils.GetFileInfoStatistics(sourceFileInfo.FullName)}\n  dest:   {FileUtils.GetFileInfoStatistics(destinationFileInfo?.FullName)}");
            }
            return true;
        }

        private static void RemoveUnmatchedIgnored(IList<FileDetail> unmatchedFiles, Action<string, int, int> updateProgress)
        {
            // delete files NOT associated with games (aka unknown files)
            // - n/a for any files that are matched as this is already considered (aka removed) during Merge()..
            //   - IgnoreIfSmaller
            //   - IgnoreIfNotNewer
            //   - IgnoreIfContainsWords
            // - only applicable for IgnoreIfContainsWords IF the table was unmatched, since IgnoreIfContainsWords doesn't mandate a table match
            var unmatchedFilesToDelete = unmatchedFiles
                .Where(unmatchedFile => _settings.Merger.SelectedIgnoreCriteria.Contains(IgnoreCriteriaEnum.IgnoreIfContainsWords) && _settings.Merger.IgnoreIWords.Any(x => unmatchedFile.Path.ToLower().Contains(x)))
                .ToList();

            unmatchedFilesToDelete.ForEach((fileDetail, i) =>
            {
                updateProgress(Path.GetFileName(fileDetail.Path), i + 1, unmatchedFilesToDelete.Count);

                ProcessIgnore(null, IgnoreCriteriaEnum.IgnoreIfContainsWords.GetDescription(), fileDetail.HitType, fileDetail.ContentType, null, new FileInfo(fileDetail.Path!), null);

                fileDetail.Deleted = true;
            });

            // log unmatched files that weren't considered for deletion - e.g. new table files, random files, etc
            var unmatchedFilesNotDeleted = unmatchedFiles.Except(unmatchedFilesToDelete);
            unmatchedFilesNotDeleted.ForEach(fileDetail =>
            {
                Logger.Info($"Skipping (unmatched file, doesn't satisfy ignore criteria).. table: n/a, description: n/a, type: {fileDetail.HitType.GetDescription()}, content: {fileDetail.ContentType.GetDescription()}");
                Logger.Debug($"- ignored..\n  source: {FileUtils.GetFileInfoStatistics(fileDetail.Path)}\n  dest:   {FileUtils.GetFileInfoStatistics(null)}");
            });
        }

        private static readonly Models.Settings.Settings _settings;
    }
}