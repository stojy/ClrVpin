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
        public ScannerStatistics(ObservableCollection<Game> games, Stopwatch scanStopWatch, ICollection<FileDetail> unknownFiles, ICollection<FixFileDetail> fixFiles)
        {
            _scanStopWatch = scanStopWatch;
            _games = games;

            CreateStatistics(unknownFiles, fixFiles);
        }

        public string Statistics { get; set; }

        public void Show(Window parentWindow, Window resultsWindow)
        {
            _window = new Window
            {
                Owner = parentWindow,
                Title = "Scanner Statistics",
                Left = resultsWindow.Left,
                Top = resultsWindow.Top + resultsWindow.Height + 10,
                SizeToContent = SizeToContent.Width,
                MinWidth = 400,
                Height = 800,
                Content = this,
                ContentTemplate = parentWindow.Owner.FindResource("ScannerStatisticsTemplate") as DataTemplate
            };
            _window.Show();
        }

        public void Close()
        {
            _window.Close();
        }

        private void CreateStatistics(ICollection<FileDetail> unknownFiles, ICollection<FixFileDetail> fixFiles)
        {
            Statistics =
                $"{CreateHitTypeStatistics()}\n" +
                $"{CreateTotalStatistics(unknownFiles, fixFiles)}";
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

        private string CreateTotalStatistics(ICollection<FileDetail> unknownFiles, ICollection<FixFileDetail> fixFiles)
        {
            var validHits = _games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitType.Valid).ToList();

            // todo; filter hit type
            var eligibleHits = _games.Count * Config.CheckContentTypes.Count;

            var fixFilesIgnored = fixFiles.Where(x => !x.Deleted).ToList();
            var fixFilesIgnoredSize = fixFilesIgnored.Sum(x => x.Size);
            var fixFilesDeleted = fixFiles.Where(x => x.Deleted).ToList();
            var fixFilesDeletedSize = fixFilesDeleted.Sum(x => x.Size);

            return "\n-----------------------------------------------\n" +
                   $"\n{"Total Games",StatisticsKeyWidth}{_games.Count}" +
                   $"\n{"Total Content",StatisticsKeyWidth}{_games.Count * Content.Types.Length}" +
                   $"\n{"Total Scanned Content",StatisticsKeyWidth}{eligibleHits}" +
                   $"\n\n{"Valid Files",StatisticsKeyWidth}{CreateFileStatistic(validHits.Count, validHits.Sum(x => x.Size))}" +
                   $"\n{"Valid Collection",StatisticsKeyWidth}{validHits.Count}/{eligibleHits} ({(decimal) validHits.Count / eligibleHits:P2})" +
                   $"\n\n{"Unknown Files",StatisticsKeyWidth}{CreateFileStatistic(unknownFiles.Count, unknownFiles.Sum(x => x.Size))}" +
                   $"\n{"Ignored Files",StatisticsKeyWidth}{CreateFileStatistic(fixFilesIgnored.Count, fixFilesIgnoredSize)}" +
                   $"\n{"Deleted Files",StatisticsKeyWidth}{CreateFileStatistic(fixFilesDeleted.Count, fixFilesDeletedSize)}" +
                   $"\n\n{"Time Taken",StatisticsKeyWidth}{_scanStopWatch.Elapsed.TotalSeconds:f2}s";
        }

        private string CreateFileStatistic(long count, long size) => $"{count} ({(size == 0 ? "0 B" : ByteSize.FromBytes(size).ToString("0.#"))})";

        private readonly ObservableCollection<Game> _games;
        private readonly Stopwatch _scanStopWatch;
        private Window _window;

        private const int StatisticsKeyWidth = -30;
    }
}