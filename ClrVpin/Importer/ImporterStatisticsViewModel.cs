using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ClrVpin.Controls;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Shared.Database;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    [AddINotifyPropertyChangedInterface]
    public class ImporterStatisticsViewModel
    {
        public ImporterStatisticsViewModel(List<Game> games, List<OnlineGame> onlineGames, TimeSpan elapsedTime, Dictionary<string, int> feedFixStatistics, ImporterMatchStatistics matchStatistics)
        {
            _games = games;
            _onlineGames = onlineGames;
            _elapsedTime = elapsedTime;
            _feedFixStatistics = feedFixStatistics;
            _matchStatistics = matchStatistics.ToDictionary();
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
                Width = 750,
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

            var onlineTotalCount = _onlineGames.Count;
            var onlineManufacturedCount = _onlineGames.Count(game => !game.IsOriginal);
            var onlineOriginalCount = _onlineGames.Count(game => game.IsOriginal);

            var localTotalCount = _games.Count;
            var localManufacturedCount = _games.Count(game => !game.Derived.IsOriginal);
            var localOriginalCount = _games.Count(game => game.Derived.IsOriginal);

            return "Feed Fixes" +
                   $"\n{feedFixStatistics}" +

                   "\n\nMatched Tables (exists in both Local and Online Databases)" +
                   CreatePercentageStatistic("Total", _matchStatistics[ImporterMatchStatistics.MatchedTotal], onlineTotalCount) +
                   CreatePercentageStatistic("Manufactured", _matchStatistics[ImporterMatchStatistics.MatchedManufactured], onlineManufacturedCount) +
                   CreatePercentageStatistic("Originals", _matchStatistics[ImporterMatchStatistics.MatchedOriginal], onlineOriginalCount) +

                   "\n\nUnmatched Online Tables (exists only in Online Database)" +
                   CreatePercentageStatistic("Total", _matchStatistics[ImporterMatchStatistics.UnmatchedOnlineTotal], onlineTotalCount) +
                   CreatePercentageStatistic("Manufactured", _matchStatistics[ImporterMatchStatistics.UnmatchedOnlineManufactured], onlineManufacturedCount) +
                   CreatePercentageStatistic("Originals", _matchStatistics[ImporterMatchStatistics.UnmatchedOnlineOriginal], onlineOriginalCount) +

                   "\n\nUnmatched Local Tables (exists only in Local Database)" +
                   CreatePercentageStatistic("Total", _matchStatistics[ImporterMatchStatistics.UnmatchedLocalTotal], localTotalCount) +
                   CreatePercentageStatistic("Manufactured", _matchStatistics[ImporterMatchStatistics.UnmatchedLocalManufactured], localManufacturedCount) +
                   CreatePercentageStatistic("Originals", _matchStatistics[ImporterMatchStatistics.UnmatchedLocalOriginal], localOriginalCount) +

                   $"\n\n{"Time Taken",StatisticsKeyWidth}{_elapsedTime.TotalSeconds:f2}s";
        }

        private static string CreatePercentageStatistic(string title, int count, int totalCount) => $"\n- {title,StatisticsKeyWidth}: {count}/{totalCount} ({100f * count / totalCount:F2}%)";

        private readonly TimeSpan _elapsedTime;
        private readonly Dictionary<string, int> _feedFixStatistics;
        private readonly Dictionary<string, int> _matchStatistics;
        private readonly List<OnlineGame> _onlineGames;
        private readonly List<Game> _games;

        private const int StatisticsKeyWidth = -30;
        private const int WindowMargin = 0;
    }
}