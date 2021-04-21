﻿using System.Collections.Generic;
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
        public ScannerStatistics(ObservableCollection<Game> games, Stopwatch scanStopWatch, ICollection<FileDetail> unknownFiles, ICollection<FileDetail> deletedFiles)
        {
            _scanStopWatch = scanStopWatch;
            _games = games;
            _smellyGames = new List<Game>(games.Where(game => game.Content.SmellyHitsView.Count > 0));

            CreateStatistics(unknownFiles, deletedFiles);
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
                Height = 750,
                Content = this,
                ContentTemplate = parentWindow.Owner.FindResource("ScannerStatisticsTemplate") as DataTemplate
            };
            _window.Show();
        }

        public void Close()
        {
            _window.Close();
        }

        private void CreateStatistics(ICollection<FileDetail> unknownFiles, ICollection<FileDetail> deletedFiles)
        {
            Statistics =
                $"{CreateHitTypeStatistics()}\n" +
                $"{CreateTotalStatistics(unknownFiles, deletedFiles)}";
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

        private string CreateTotalStatistics(ICollection<FileDetail> unknownFiles, ICollection<FileDetail> deletedFiles)
        {
            var validHits = _games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitType.Valid).ToList();

            // todo; filter hit type
            var eligibleHits = _games.Count * Config.CheckContentTypes.Count;

            return "\n-----------------------------------------------\n" +
                   $"\n{"Total Games",StatisticsKeyWidth}{_games.Count}" +
                   $"\n{"Total Content",StatisticsKeyWidth}{_games.Count * Content.Types.Length}" +
                   $"\n{"Total Scanned Content",StatisticsKeyWidth}{eligibleHits}" +
                   $"\n\n{"Valid Files",StatisticsKeyWidth}{validHits.Count}" +
                   $"\n{"Valid Files Size",StatisticsKeyWidth}{ByteSize.FromBytes(validHits.Sum(x => x.Size)).ToString("#")}" +
                   $"\n{"Valid Collection",StatisticsKeyWidth}{validHits.Count}/{eligibleHits} ({(decimal) validHits.Count / eligibleHits:P2})" +
                   $"\n\n{"Unneeded Files",StatisticsKeyWidth}{unknownFiles.Count}" +
                   $"\n{"Unneeded Files Size",StatisticsKeyWidth}{ByteSize.FromBytes(unknownFiles.Sum(x => x.Size)).ToString("#")}" +
                   $"\n\n{"Deleted Files",StatisticsKeyWidth}{deletedFiles.Count}" +
                   $"\n{"Deleted Files Size",StatisticsKeyWidth}{ByteSize.FromBytes(deletedFiles.Sum(x => x.Size)).ToString("#")}" +
                   $"\n\n{"Time Taken",StatisticsKeyWidth}{_scanStopWatch.Elapsed.TotalSeconds:f2}s";
        }

        private readonly ObservableCollection<Game> _games;
        private readonly Stopwatch _scanStopWatch;
        private List<Game> _smellyGames;
        private Window _window;

        private const int StatisticsKeyWidth = -30;
    }
}