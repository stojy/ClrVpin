using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ByteSizeLib;
using ClrVpin.Models;
using MaterialDesignExtensions.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class ScannerStatistics
    {
        public ScannerStatistics(ObservableCollection<Game> games, TimeSpan scanStopWatch, ICollection<FixFileDetail> fixFiles)
        {
            _scanStopWatch = scanStopWatch;
            _games = games;

            CreateStatistics(fixFiles);
        }

        public string Statistics { get; set; }
        public Window Window { get; private set; }

        public void Show(Window parentWindow, double left, double top, double width)
        {
            Window = new MaterialWindow
            {
                Owner = parentWindow,
                Title = "Scanner Statistics",
                Left = left,
                Top = top,
                Width = width,
                Height = 885,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ScannerStatisticsTemplate") as DataTemplate
            };
            Window.Show();
        }

        public void Close()
        {
            Window.Close();
        }

        private void CreateStatistics(ICollection<FixFileDetail> fixFiles)
        {
            Statistics =
                $"{CreateHitTypeStatistics(fixFiles)}\n" +
                $"{CreateTotalStatistics(fixFiles)}";
        }

        private string CreateHitTypeStatistics(ICollection<FixFileDetail> fixFileDetails)
        {
            // for every hit type, create stats against every content type
            var hitStatistics = Config.HitTypes.Select(hitType =>
            {

                string contents;
                if (hitType.Enum != HitTypeEnum.Unknown && hitType.Enum != HitTypeEnum.Unsupported)
                {
                    // all known content has an associated game
                    contents = string.Join("\n", Config.ContentTypes.Select(contentType =>
                        $"- {contentType.Description,StatisticsKeyWidth + 2}{GetContentStatistics(contentType.Enum, hitType.Enum, fixFileDetails)}"));
                }
                else
                {
                    // unknown matches aren't attributed to a game.. so we treat them a little differently
                    contents = string.Join("\n", Config.ContentTypes.Select(contentType =>
                        $"- {contentType.Description,StatisticsKeyWidth + 2}{GetContentUnknownStatistics(contentType.Enum, hitType.Enum, fixFileDetails)}"));
                }

                return $"{hitType.Description}\n{contents}";
            });

            return $"Criteria statistics for each content type\n\n{string.Join("\n\n", hitStatistics)}";
        }

        private string GetContentStatistics(ContentTypeEnum contentType, HitTypeEnum hitType, IEnumerable<FixFileDetail> fixFileDetails)
        {
            if (!Model.Config.CheckContentTypes.Contains(contentType.GetDescription()) || !Model.Config.CheckHitTypes.Contains(hitType))
                return "skipped";

            var renamePrefix = hitType == HitTypeEnum.Missing ? "irreparable" : Model.Config.FixHitTypes.Contains(hitType) ? "renamed" : "renamable";

            var statistics = $"{renamePrefix} {_games.Count(g => g.Content.ContentHitsCollection.First(x => x.Type == contentType).Hits.Any(hit => hit.Type == hitType))}/{_games.Count}";

            // don't show removed for missing files, since it's n/a
            if (hitType != HitTypeEnum.Missing)
                statistics += $", {CreateMissingFileStatistics(contentType, hitType, fixFileDetails)}";

            return statistics;
        }

        private static string GetContentUnknownStatistics(ContentTypeEnum contentType, HitTypeEnum hitType, IEnumerable<FixFileDetail> fixFileDetails)
        {
            if (!Model.Config.CheckContentTypes.Contains(contentType.GetDescription()) || !Model.Config.CheckHitTypes.Contains(hitType))
                return "skipped";

            return CreateMissingFileStatistics(contentType, hitType, fixFileDetails);
        }

        private static string CreateMissingFileStatistics(ContentTypeEnum contentType, HitTypeEnum hitType, IEnumerable<FixFileDetail> fixFileDetails)
        {
            var removePrefix = Model.Config.FixHitTypes.Contains(hitType) ? "removed" : "removable";
            return $"{removePrefix} {CreateFileStatistic(fixFileDetails.Where(x => x.HitType == hitType && x.ContentType == contentType).ToList())}";
        }

        private string CreateTotalStatistics(ICollection<FixFileDetail> fixFiles)
        {
            var validHits = _games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitTypeEnum.Valid).ToList();

            var eligibleHits = _games.Count * Model.Config.CheckContentTypes.Count;

            // all files
            var allFilesCount = validHits.Count + fixFiles.Count;
            var allFilesSize = validHits.Sum(x => x.Size) + fixFiles.Sum(x => x.Size) ?? 0;

            // renamed
            // - must be configured as a fix hit type
            // - unknown is n/a apply for renamable, i.e. since we don't know what game/table to rename it to
            var fixFilesRenamed = fixFiles.Where(x => x.Renamed).ToList();
            var fixFilesRenamedSize = fixFilesRenamed.Sum(x => x.Size);

            // removed (deleted)
            // - must be configured as a fix hit type
            var fixFilesDeleted = fixFiles.Where(x => x.Deleted).ToList();
            var fixFilesDeletedSize = fixFilesDeleted.Sum(x => x.Size);
            var fixFilesDeletedUnknown = fixFilesDeleted.Where(x => x.HitType == HitTypeEnum.Unknown).ToList();
            var fixFilesDeletedUnknownSize = fixFilesDeletedUnknown.Sum(x => x.Size);

            // ignored (removable and renamable)
            // - includes renamable AND removable files
            // - unknown..
            //   - n/a apply for renamable, i.e. since we don't know what game/table to rename it to
            //   - applicable for removable
            var fixFilesIgnored = fixFiles.Where(x => x.Ignored).ToList();
            var fixFilesIgnoredSize = fixFilesIgnored.Sum(x => x.Size);
            var fixFilesIgnoredUnknown = fixFilesIgnored.Where(x => x.HitType == HitTypeEnum.Unknown).ToList();
            var fixFilesIgnoredUnknownSize = fixFilesIgnoredUnknown.Sum(x => x.Size);

            return "\n-----------------------------------------------\n" +
                   "\nTotals" +
                   $"\n{"- Available Tables",StatisticsKeyWidth}{_games.Count}" +
                   $"\n{"- Possible Content",StatisticsKeyWidth}{_games.Count * Config.ContentTypes.Length}" +
                   $"\n{"- Checked Content",StatisticsKeyWidth}{eligibleHits}" +
                   $"\n\n{"All Files",StatisticsKeyWidth}{CreateFileStatistic(allFilesCount, allFilesSize)}" +
                   $"\n\n{"Valid Files",StatisticsKeyWidth}{CreateFileStatistic(validHits.Count, validHits.Sum(x => x.Size ?? 0))}" +
                   $"\n{"- Collection",StatisticsKeyWidth}{validHits.Count}/{eligibleHits} ({(decimal) validHits.Count / eligibleHits:P2})" +
                   $"\n\n{"Fixed/Fixable Files",StatisticsKeyWidth}{CreateFileStatistic(fixFiles.Count, fixFiles.Sum(x => x.Size))}" +
                   $"\n{"- renamed",StatisticsKeyWidth}{CreateFileStatistic(fixFilesRenamed.Count, fixFilesRenamedSize)}" +
                   $"\n{"- removed",StatisticsKeyWidth}{CreateFileStatistic(fixFilesDeleted.Count, fixFilesDeletedSize)}" +
                   $"\n{"  (criteria: unknown)",StatisticsKeyWidth}{CreateFileStatistic(fixFilesDeletedUnknown.Count, fixFilesDeletedUnknownSize)}" +
                   $"\n{"- renamable and removable",StatisticsKeyWidth}{CreateFileStatistic(fixFilesIgnored.Count, fixFilesIgnoredSize)}" +
                   $"\n{"  (criteria: unknown)",StatisticsKeyWidth}{CreateFileStatistic(fixFilesIgnoredUnknown.Count, fixFilesIgnoredUnknownSize)}" +
                   $"\n\n{"Time Taken",StatisticsKeyWidth}{_scanStopWatch.TotalSeconds:f2}s";
        }

        private static string CreateFileStatistic(ICollection<FixFileDetail> removedFiles)
        {
            return CreateFileStatistic(removedFiles.Count, removedFiles.Sum(x => x.Size));
        }

        private static string CreateFileStatistic(long count, long size) => $"{count} ({(size == 0 ? "0 B" : ByteSize.FromBytes(size).ToString("0.#"))})";

        private readonly ObservableCollection<Game> _games;
        private readonly TimeSpan _scanStopWatch;

        private const int StatisticsKeyWidth = -26;
    }
}