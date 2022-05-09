using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ClrVpin.Controls;
using ClrVpin.Models.Importer.Vps;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    [AddINotifyPropertyChangedInterface]
    public class ImporterStatisticsViewModel
    {
        public ImporterStatisticsViewModel(List<OnlineGame> onlineGames, TimeSpan elapsedTime, Dictionary<string, int> feedFixStatistics, Dictionary<string, int> matchStatistics)
        {
            _onlineGames = onlineGames;
            _elapsedTime = elapsedTime;
            _feedFixStatistics = feedFixStatistics;
            _matchStatistics = matchStatistics;
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
            
            var totalGamesCount = _onlineGames.Count;
            var manufacturedGamesCount = _onlineGames.Count(game => !game.IsOriginal);
            var originalGamesCount = _onlineGames.Count(game => game.IsOriginal);

            return $"Feed Fixes" +
                   $"\n{feedFixStatistics}" +
                   $"\n\nMatched Local and Online Database" +
                   CreatePercentageStatistic("Total", _matchStatistics[ImporterUtils.MatchMatchedTotal], totalGamesCount) +
                   CreatePercentageStatistic("Manufactured", _matchStatistics[ImporterUtils.MatchMatchedManufactured], manufacturedGamesCount) +
                   CreatePercentageStatistic("Originals", _matchStatistics[ImporterUtils.MatchMatchedOriginal], originalGamesCount) +
                   CreatePercentageStatistic("Missing", _matchStatistics[ImporterUtils.MatchUnmatchedTotal], totalGamesCount) +
                   $"\n\n{"Time Taken",StatisticsKeyWidth}{_elapsedTime.TotalSeconds:f2}s";
        }

        private static string CreatePercentageStatistic(string title, int count, int totalCount) => $"\n- {title,StatisticsKeyWidth}: {count}/{totalCount} ({100f * count / totalCount:F2}%)";

        private readonly TimeSpan _elapsedTime;
        private readonly Dictionary<string, int> _feedFixStatistics;
        private readonly Dictionary<string, int> _matchStatistics;
        private readonly List<OnlineGame> _onlineGames;

        private const int StatisticsKeyWidth = -30;
        private const int WindowMargin = 0;
    }
}