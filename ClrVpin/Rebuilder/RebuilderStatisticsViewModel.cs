using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ClrVpin.Models;
using ClrVpin.Models.Settings;
using ClrVpin.Shared;
using MaterialDesignExtensions.Controls;

namespace ClrVpin.Rebuilder
{
    public sealed class RebuilderStatisticsViewModel : StatisticsViewModel
    {
        public RebuilderStatisticsViewModel(ObservableCollection<Game> games, TimeSpan elapsedTime, ICollection<FileDetail> gameFiles, ICollection<FileDetail> unknownFiles)
            : base(games, elapsedTime, gameFiles, unknownFiles)
        {
            // hit type stats for all supported types only
            // - including the extra 'under the hood' types.. valid, unknown, unsupported
            SupportedHitTypes = StaticSettings.MatchTypes.ToList();

            // content type stats for the single content type being rebuilt
            SupportedContentTypes = new List<ContentType> {Settings.GetSelectedDestinationContentType()};

            // rebuilder only supports a single selected content type
            SelectedCheckContentTypes = new List<string> {Settings.Rebuilder.DestinationContentType};

            // rebuilder doesn't support check and fix separately
            SelectedCheckHitTypes = Settings.Rebuilder.SelectedMatchTypes.ToList();
            SelectedFixHitTypes = SelectedCheckHitTypes;

            // unlike scanner, the total count represents the number of files that were analyzed
            TotalCount = GameFiles.Count + UnknownFiles.Count;
        }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindow
            {
                Owner = parentWindow,
                Title = "Rebuilder Statistics",
                Left = left,
                Top = top,
                Width = 750,
                Height = Model.ScreenWorkArea.Height - 10,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("RebuilderStatisticsTemplate") as DataTemplate
            };
            Window.Show();

            CreateStatistics();
        }

        protected override string CreateTotalStatistics()
        {
            // all files
            var allFiles = GameFiles.Concat(UnknownFiles).ToList();
            var mergedFiles = allFiles.Where(x => !x.Ignored);
            var notMergedFiles = allFiles.Where(x => x.Ignored);

            return "\n-----------------------------------------------\n" +
                   $"\n{"Source Files",StatisticsKeyWidth}" +
                   $"\n{"- Total",StatisticsKeyWidth}{CreateFileStatistic(allFiles)}" +
                   $"\n{"- Merged",StatisticsKeyWidth}{CreateFileStatistic(mergedFiles)}" +
                   $"\n{"- Ignored: Matched",StatisticsKeyWidth}{CreateFileStatistic(GameFiles.Where(x => x.Ignored))}" +
                   $"\n{"- Ignored: Unmatched",StatisticsKeyWidth}{CreateFileStatistic(UnknownFiles.Where(x => x.Ignored))}" +
                   "\n" +
                   $"\n{"Time Taken",StatisticsKeyWidth}{ElapsedTime.TotalSeconds:f2}s";
        }
    }
}