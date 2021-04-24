using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using ByteSizeLib;
using ClrVpin.Models;
using PropertyChanged;
using Utils;

namespace ClrVpin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class ScannerStatistics
    {
        public ScannerStatistics(ObservableCollection<Game> games, Stopwatch scanStopWatch, ICollection<FixFileDetail> fixFiles)
        {
            _scanStopWatch = scanStopWatch;
            _games = games;

            CreateStatistics(fixFiles);
        }

        public string Statistics { get; set; }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new Window
            {
                Owner = parentWindow,
                Title = "Scanner Statistics",
                Left = left,
                Top = top,
                Width = 400,
                Height = 900,
                Content = this,
                ContentTemplate = parentWindow.Owner.FindResource("ScannerStatisticsTemplate") as DataTemplate
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
                $"{CreateHitTypeStatistics()}\n" +
                $"{CreateTotalStatistics(fixFiles)}";
        }

        private string CreateHitTypeStatistics()
        {
            // for every hit type, create stats against every content type
            var hitStatistics = Hit.Types.Select(hitType =>
            {
                var title = $"{hitType.GetDescription()}";

                var contents = string.Join("\n",
                    Content.Types.Select(type =>
                        $"- {type,StatisticsKeyWidth + 2}{GetSmellyStatistics(type, hitType)}"));
                return $"{title}\n{contents}";
            });

            return $"{string.Join("\n\n", hitStatistics)}";
        }

        private string GetSmellyStatistics(string contentType, HitType hitType)
        {
            if (!Config.CheckContentTypes.Contains(contentType) || !Config.CheckHitTypes.Contains(hitType))
                return "skipped";

            return _games.Count(g => g.Content.ContentHitsCollection.First(x => x.Type == contentType).Hits.Any(hit => hit.Type == hitType)) + "/" + _games.Count;
        }

        private string CreateTotalStatistics(ICollection<FixFileDetail> fixFiles)
        {
            var validHits = _games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitType.Valid).ToList();

            var eligibleHits = _games.Count * Config.CheckContentTypes.Count;

            var fixFilesIgnored = fixFiles.Where(x => x.Ignored).ToList();
            var fixFilesIgnoredSize = fixFilesIgnored.Sum(x => x.Size);
            var fixFilesDeleted = fixFiles.Where(x => x.Deleted).ToList();
            var fixFilesDeletedSize = fixFilesDeleted.Sum(x => x.Size);
            var fixFilesRenamed = fixFiles.Where(x => x.Renamed).ToList();
            var fixFilesRenamedSize = fixFilesRenamed.Sum(x => x.Size);
            
            // identify unknown fix files for separate statistics
            var unknownFiles = fixFiles.Where(x => x.HitType == HitType.Unknown).ToList();
            var unknownFilesIgnored = unknownFiles.Where(x => x.Ignored && !x.Renamed).ToList();
            var unknownFilesIgnoredSize = unknownFilesIgnored.Sum(x => x.Size);
            var unknownFilesDeleted = unknownFiles.Where(x => x.Deleted).ToList();
            var unknownFilesDeletedSize = unknownFilesDeleted.Sum(x => x.Size);

            return "\n-----------------------------------------------\n" +
                   "\nTotals" +
                   $"\n{"- Available Games",StatisticsKeyWidth}{_games.Count}" +
                   $"\n{"- Possible Content",StatisticsKeyWidth}{_games.Count * Content.Types.Length}" +
                   $"\n{"- Checked Content",StatisticsKeyWidth}{eligibleHits}" +
                   $"\n\n{"Valid Files",StatisticsKeyWidth}{CreateFileStatistic(validHits.Count, validHits.Sum(x => x.Size ?? 0))}" +
                   $"\n{"- Valid Collection",StatisticsKeyWidth}{validHits.Count}/{eligibleHits} ({(decimal) validHits.Count / eligibleHits:P2})" +
                   $"\n\n{"Fixable Files",StatisticsKeyWidth}{CreateFileStatistic(fixFiles.Count, fixFiles.Sum(x => x.Size))}" +
                   $"\n{"- Renamed Files",StatisticsKeyWidth}{CreateFileStatistic(fixFilesRenamed.Count, fixFilesRenamedSize)}" +
                   $"\n{"- Deleted Files",StatisticsKeyWidth}{CreateFileStatistic(fixFilesDeleted.Count, fixFilesDeletedSize)}" +
                   $"\n{"  (Unknown Files)",StatisticsKeyWidth}{CreateFileStatistic(unknownFilesDeleted.Count, unknownFilesDeletedSize)}" +
                   $"\n{"- Ignored Files",StatisticsKeyWidth}{CreateFileStatistic(fixFilesIgnored.Count, fixFilesIgnoredSize)}" +
                   $"\n{"  (Unknown Files)",StatisticsKeyWidth}{CreateFileStatistic(unknownFilesIgnored.Count, unknownFilesIgnoredSize)}" +
                   $"\n\n{"Time Taken",StatisticsKeyWidth}{_scanStopWatch.Elapsed.TotalSeconds:f2}s";
        }

        private static string CreateFileStatistic(long count, long size) => $"{count} ({(size == 0 ? "0 B" : ByteSize.FromBytes(size).ToString("0.#"))})";

        private readonly ObservableCollection<Game> _games;
        private readonly Stopwatch _scanStopWatch;
        public Window Window { get; private set; }

        private const int StatisticsKeyWidth = -25;
    }
}