using System.Collections;
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
        public ScannerStatistics(ObservableCollection<Game> games, Stopwatch scanStopWatch, ICollection unknownFiles)
        {
            _scanStopWatch = scanStopWatch;
            _games = games;

            CreateStatistics(unknownFiles);
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
                Height = 650,
                Content = this,
                ContentTemplate = parentWindow.Owner.FindResource("ScannerStatisticsTemplate") as DataTemplate
            };
            _window.Show();
        }

        public void Close()
        {
            _window.Close();
        }

        private void CreateStatistics(ICollection unknownFiles)
        {
            Statistics =
                $"{CreateHitTypeStatistics()}\n" +
                $"{CreateTotalStatistics(unknownFiles)}";
        }

        private string CreateHitTypeStatistics()
        {
            // for every hit type, create stats against every content type
            var hitStatistics = Hit.Types.Select(hitType =>
            {
                var title = $"{hitType.GetDescription()}";

                //var contents = string.Join("\n",
                //    Content.Types.Select(type =>
                //        $"- {type,StatisticsKeyWidth + 2}{SmellyGames.Count(g => g.Content.ContentHitsCollection.First(x => x.Type == type).Hits.Any(hit => hit.Type == hitType))}/{Games.Count}"));
                var contents = string.Join("\n",
                    Content.Types.Select(type =>
                        $"- {type,StatisticsKeyWidth + 2}{_games.Count(g => g.Content.ContentHitsCollection.First(x => x.Type == type).Hits.Any(hit => hit.Type == hitType))}/{_games.Count}"));
                return $"{title}\n{contents}";
            });

            return $"{string.Join("\n\n", hitStatistics)}";
        }

        private string CreateTotalStatistics(ICollection unknownFiles)
        {
            var validHits = _games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitType.Valid).ToList();

            return "\n-----------------------------------------------\n" +
                   $"\n{"Total Games",StatisticsKeyWidth}{_games.Count}" +
                   $"\n{"Unneeded Files",StatisticsKeyWidth}{unknownFiles.Count}" +
                   $"\n{"Valid Files",StatisticsKeyWidth}{validHits.Count}/{_games.Count * Content.Types.Length} ({(decimal) validHits.Count / (_games.Count * Content.Types.Length):P2})" +
                   $"\n{"Valid Files Size",StatisticsKeyWidth}{ByteSize.FromBytes(validHits.Sum(x => x.Size)).ToString("#")}" +
                   $"\n\n{"Time Taken",StatisticsKeyWidth}{_scanStopWatch.Elapsed.TotalSeconds:f2}s";
        }

        private readonly ObservableCollection<Game> _games;
        private readonly Stopwatch _scanStopWatch;
        private Window _window;

        private const int StatisticsKeyWidth = -30;
    }
}