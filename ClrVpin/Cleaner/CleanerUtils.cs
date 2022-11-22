using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClrVpin.Logging;
using ClrVpin.Models.Cleaner;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using ClrVpin.Shared.Fuzzy;
using Utils.Extensions;

namespace ClrVpin.Cleaner;

public static class CleanerUtils
{
    private static readonly Models.Settings.Settings _settings = Model.Settings;

    public static async Task<List<FileDetail>> FixAsync(List<LocalGame> games, string backupFolder, Action<string, float> updateProgress)
    {
        var fixedFileDetails = await Task.Run(() => Fix(games, backupFolder, updateProgress));
        return fixedFileDetails;
    }

    private static List<FileDetail> Fix(ICollection<LocalGame> localGames, string backupFolder, Action<string, float> updateProgress)
    {
        FileUtils.SetActiveBackupFolder(backupFolder);

        // EVERY GAME THAT HAS A HIT (IRRESPECTIVE OF MATCH CRITERIA) WILL HAVE A GAME FILE RETURNED, i.e. irrespective of whether..
        // - match criteria is selected or relevant
        // - skip criteria is selected or relevant
        var gameFiles = new List<FileDetail>();

        var selectedContentTypes = _settings.GetSelectedCheckContentTypes().ToList();

        var gamesWithContentCount = 0;
        var gamesWithContentMaxCount = 0;
            
        static bool GamesWithContentPredicate(LocalGame localGame, ContentType contentType) => localGame.Content.ContentHitsCollection.Any(contentHits => contentHits.ContentType == contentType && contentHits.Hits.Any(hit => hit.Type != HitTypeEnum.Missing));

        selectedContentTypes.ForEach(contentType =>
        {
            gamesWithContentMaxCount += localGames.Count(game => GamesWithContentPredicate(game, contentType));
        });

        // iterate through each selected content type
        selectedContentTypes.ForEach(contentType =>
        {
            // fixable localGame exclude following hit types..
            // - missing - associated with localGame as the default entry
            //   - can be fixed if other (non-correct name) matches are available (e.g. fuzzy match( but can't be fixed.. requires file to be downloaded
            //   - if no other matches exist, then the content can't be fixed as the content needs to be downloaded
            // - unknown - not associated with a localGame (i.e. no need to check here).. handled elsewhere
            // - unsupported - not associated with any known content type, e.g. Magic.ini
            var fixableContentLocalGames = localGames.Where(localGame => GamesWithContentPredicate(localGame, contentType)).ToList();

            // fix files associated with localGame, if they satisfy the fix criteria
            fixableContentLocalGames.ForEach(fixableContentLocalGame =>
            {
                updateProgress(fixableContentLocalGame.Game.Description, ++gamesWithContentCount / (float)gamesWithContentMaxCount);

                var gameContentHits = fixableContentLocalGame.Content.ContentHitsCollection.First(contentHits => contentHits.ContentType == contentType);

                // the underlying HitTypeEnum is declared in descending priority order
                var orderedByHitType = gameContentHits.Hits.OrderBy(hit => hit.Type);

                // when fixing order, need to cater for no FileInfo, e.g. file missing
                switch (_settings.Cleaner.SelectedMultipleMatchOption)
                {
                    case MultipleMatchOptionEnum.PreferCorrectName:
                        FixOrderedHits(orderedByHitType.ToList(), gameFiles, fixableContentLocalGame);
                        break;
                    case MultipleMatchOptionEnum.PreferLargestSize:
                        FixOrderedHits(orderedByHitType.OrderByDescending(hit => hit.FileInfo?.Length).ToList(), gameFiles, fixableContentLocalGame);
                        break;
                    case MultipleMatchOptionEnum.PreferMostRecent:
                        FixOrderedHits(orderedByHitType.OrderByDescending(hit => hit.FileInfo?.LastWriteTime).ToList(), gameFiles, fixableContentLocalGame);
                        break;
                    case MultipleMatchOptionEnum.PreferMostRecentAndExceedSizeThreshold:
                        var orderedByMostRecent = orderedByHitType.OrderByDescending(hit => hit.FileInfo?.LastWriteTime).ToList();

                        // if the correct name file exists, then apply additional ordering to filter out (aka de-prioritize) files that don't exceed the threshold
                        decimal? correctHitLength = orderedByMostRecent.FirstOrDefault(x => x.Type == HitTypeEnum.CorrectName)?.FileInfo.Length;
                        if (correctHitLength != null)
                        {
                            var sizeThreshold = _settings.Cleaner.MultipleMatchExceedSizeThresholdPercentage / 100;
                            orderedByMostRecent = orderedByMostRecent.OrderByDescending(hit => correctHitLength != 0 && hit.FileInfo?.Length / correctHitLength > sizeThreshold).ToList();
                        }

                        FixOrderedHits(orderedByMostRecent.ToList(), gameFiles, fixableContentLocalGame);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        });

        return gameFiles;
    }

    private static void FixOrderedHits(ICollection<Hit> orderedHits, List<FileDetail> gameFiles, LocalGame localGame)
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

        var multiOptionDescription = _settings.Cleaner.SelectedMultipleMatchOption.GetDescription();
        if (_settings.Cleaner.SelectedMultipleMatchOption == MultipleMatchOptionEnum.PreferMostRecentAndExceedSizeThreshold)
            multiOptionDescription = $"{multiOptionDescription} (criteria: {_settings.Cleaner.MultipleMatchExceedSizeThresholdPercentage / 100:P2})";

        // nothing to fix if the preferred hit 'correct name' AND the other hits aren't selected
        // - e.g. preferred = wrong case, other=correct name (not selected)
        if (preferredHit.Type == HitTypeEnum.CorrectName && !nonPreferredHits.Any(hit => hit.Type.In(_settings.Cleaner.SelectedFixHitTypes)))
        {
            Logger.Info($"Skipping (fix criteria not selected).. table: {localGame.Game.Name}, description: {localGame.Game.Description}, " +
                        $"preferred type: {preferredHit.Type.GetDescription()}, required fix types (unselected): {string.Join('|', nonPreferredHits.Select(x => x.Type.GetDescription()).Distinct())}, " +
                        $"content: {preferredHit.ContentType}, multi option: {multiOptionDescription}");
            return;
        }

        // nothing to fix if the preferred hit isn't selected
        // - e.g. correct name not selected
        if (preferredHit.Type != HitTypeEnum.CorrectName && !preferredHit.Type.In(_settings.Cleaner.SelectedFixHitTypes))
        {
            Logger.Info($"Skipping (fix criteria not selected).. table: {localGame.Game.Name}, description: {localGame.Game.Description}, " +
                        $"preferred type (unselected): {preferredHit.Type.GetDescription()}, " +
                        $"content: {preferredHit.ContentType}, multi option: {multiOptionDescription}");
            return;
        }

        // delete all hit files except the first
        Logger.Info($"Fixing.. table: {localGame.Game.Name}, description: {localGame.Game.Description}, type: {preferredHit.Type.GetDescription()}, content: {preferredHit.ContentType}, multi option: {multiOptionDescription}",
            isHighlight: true);

        var (description, warning) = Fuzzy.GetScoreDetail(preferredHit.Score);
        var message = $"- matched (score: {description})..\n  source: {FileUtils.GetFileInfoStatistics(preferredHit.Path)}";
        if (warning)
            Logger.Warn(message);
        else
            Logger.Debug(message);

        gameFiles.AddRange(FileUtils.DeleteAllExcept(orderedHits, preferredHit, _settings.Cleaner.SelectedFixHitTypes));

        // if the preferred hit file isn't 'CorrectName', then rename it
        if (preferredHit.Type != HitTypeEnum.CorrectName)
            gameFiles.Add(FileUtils.Rename(preferredHit, localGame, _settings.Cleaner.SelectedFixHitTypes, _settings.GetContentType(preferredHit.ContentTypeEnum).KindredExtensionsList));
    }

    public static async Task RemoveUnmatchedAsync(List<FileDetail> unmatchedFiles, Action<string, float> updateProgress)
    {
        await Task.Run(() => RemoveUnmatched(unmatchedFiles, updateProgress));
    }

    private static void RemoveUnmatched(IEnumerable<FileDetail> unmatchedFiles, Action<string, float> updateProgress)
    {
        // delete files NOT associated with localGame, aka unmatched files
        var unmatchedFilesToDelete = unmatchedFiles.Where(unmatchedFile =>
            unmatchedFile.HitType == HitTypeEnum.Unknown && _settings.Cleaner.SelectedFixHitTypes.Contains(HitTypeEnum.Unknown) ||
            unmatchedFile.HitType == HitTypeEnum.Unsupported && _settings.Cleaner.SelectedFixHitTypes.Contains(HitTypeEnum.Unsupported)).ToList();

        unmatchedFilesToDelete.ForEach((fileDetail, i) =>
        {
            updateProgress(Path.GetFileName(fileDetail.Path), (i+1f) / unmatchedFilesToDelete.Count);

            var contentType = fileDetail.ContentType.GetDescription();
            Logger.Info($"Fixing (unmatched).. table: n/a, description: n/a, type: {fileDetail.HitType.GetDescription()}, content: {contentType}");
            FileUtils.Delete(fileDetail.Path, fileDetail.HitType, contentType);

            fileDetail.Deleted = true;
        });
    }
}