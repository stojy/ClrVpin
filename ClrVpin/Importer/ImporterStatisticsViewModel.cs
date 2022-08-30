using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ClrVpin.Controls;
using ClrVpin.Models.Importer;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    [AddINotifyPropertyChangedInterface]
    public class ImporterStatisticsViewModel
    {
        public ImporterStatisticsViewModel(IList<GameItem> gameItems, TimeSpan elapsedTime, Dictionary<string, int> feedFixStatistics)
        {
            _gameItems = gameItems;
            _elapsedTime = elapsedTime;
            _feedFixStatistics = feedFixStatistics;
        }

        public Window Window { get; private set; }

        public string Result { get; set; }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindowEx
            {
                Owner = parentWindow,
                Title = "Importer Statistics",
                Left = left,
                Top = top,
                Width = 712,
                Height = Model.ScreenWorkArea.Height - top - WindowMargin,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ImporterStatisticsTemplate") as DataTemplate
            };
            Window.Show();

            //CreateStatistics();
            Result = CreateTotalStatistics();
        }

        public void Close() => Window.Close();


        private string CreateTotalStatistics()
        {
            var feedFixStatistics = _feedFixStatistics.Select(kv => $"- {kv.Key,StatisticsKeyWidth}: {kv.Value}").StringJoin("\n");

            var matchedItems = _gameItems.Where(gameItem => gameItem.TableMatchType == TableMatchOptionEnum.LocalAndOnline).ToList();
            var unmatchedItems = _gameItems.Where(gameItem => gameItem.TableMatchType == TableMatchOptionEnum.LocalOnly).ToList();
            var missingItems = _gameItems.Where(gameItem => gameItem.TableMatchType == TableMatchOptionEnum.OnlineOnly).ToList();
            var matchedAndMissingItems = matchedItems.Concat(missingItems).ToList();

            return "Feed Fixes" +
                   $"\n{feedFixStatistics}" +
                   "\n\nAll Tables" +
                   CreateCountMatchTypeGrouping(_gameItems, matchedItems, missingItems, unmatchedItems) +
                   "\n\nMatched Tables (exists in both Local and Online Databases)" +
                   CreatePercentageGrouping(matchedItems, matchedAndMissingItems) +
                   "\n\nMissing Tables (exists only in Online Database)" +
                   CreatePercentageGrouping(missingItems, matchedAndMissingItems) +
                   "\n\nUnmatched Tables (exists only in Local Database)" +
                   CreateCountGrouping(unmatchedItems) +
                   $"\n\n{"Time Taken",StatisticsKeyWidth}{_elapsedTime.TotalSeconds:f2}s";
        }

        private static string CreateCountMatchTypeGrouping(ICollection<GameItem> allItems, ICollection<GameItem> matchedItems, ICollection<GameItem> missingItems, ICollection<GameItem> unmatchedItems)
        {
            return CreateCountStatistic("Total", allItems.Count) +
                   CreateCountStatistic("Matched", matchedItems.Count) +
                   CreateCountStatistic("Missing", missingItems.Count) +
                   CreateCountStatistic("Unmatched", unmatchedItems.Count);
        }

        private static string CreateCountGrouping(ICollection<GameItem> gameItems)
        {
            return CreateCountStatistic("Total", gameItems.Count) +
                   CreateCountStatistic("Manufactured", gameItems.Count(item => !item.IsOriginal)) +
                   CreateCountStatistic("Original", gameItems.Count(item => item.IsOriginal));
        }

        private static string CreatePercentageGrouping(ICollection<GameItem> gameItems, ICollection<GameItem> matchedAndMissingItems)
        {
            return CreatePercentageStatistic("Total", gameItems.Count, matchedAndMissingItems.Count) +
                   CreatePercentageStatistic("Manufactured", gameItems.Count(item => !item.IsOriginal), matchedAndMissingItems.Count(item => !item.IsOriginal)) +
                   CreatePercentageStatistic("Original", gameItems.Count(item => item.IsOriginal), matchedAndMissingItems.Count(item => item.IsOriginal));
        }

        private static string CreatePercentageStatistic(string title, int count, int totalCount) => $"\n- {title,StatisticsKeyWidth}: {count}/{totalCount} ({100f * count / totalCount:F2}%)";
        private static string CreateCountStatistic(string title, int count) => $"\n- {title,StatisticsKeyWidth}: {count}";

        private readonly TimeSpan _elapsedTime;
        private readonly Dictionary<string, int> _feedFixStatistics;
        private readonly IList<GameItem> _gameItems;

        private const int StatisticsKeyWidth = -35;
        private const int WindowMargin = 0;
    }
}