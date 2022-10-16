using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ClrVpin.Controls;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;

namespace ClrVpin.Rebuilder
{
    public sealed class RebuilderStatisticsViewModel : StatisticsViewModel
    {
        public RebuilderStatisticsViewModel(ObservableCollection<LocalGame> games, TimeSpan elapsedTime, ICollection<FileDetail> fixedFiles, ICollection<FileDetail> unmatchedFiles)
            : base(games, elapsedTime, fixedFiles, unmatchedFiles)
        {
            // hit type stats for all supported types only
            // - including the extra 'under the hood' types.. valid, unknown, unsupported
            SupportedHitTypes = StaticSettings.MatchTypes.ToList();

            // content type stats for the single content type being rebuilt
            SupportedContentTypes = new List<ContentType> { Settings.GetSelectedDestinationContentType() };

            // rebuilder only supports a single selected content type
            SelectedCheckContentTypes = new List<string> { Settings.Rebuilder.DestinationContentType };

            // rebuilder doesn't support check and fix separately
            SelectedCheckHitTypes = Settings.Rebuilder.SelectedMatchTypes.ToList();
            SelectedFixHitTypes = SelectedCheckHitTypes;

            // unlike scanner, the total count represents the number of files that were analyzed
            TotalCount = FixedFiles.Count + UnmatchedFiles.Count;
        }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindowEx
            {
                Owner = parentWindow,
                Title = "Rebuilder Statistics",
                Left = left,
                Top = top,
                Width = 750,
                Height = Model.ScreenWorkArea.Height - WindowMargin - WindowMargin,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("RebuilderStatisticsTemplate") as DataTemplate
            };
            Window.Show();

            CreateStatistics();
        }

        protected override string CreateTotalStatistics()
        {
            return "\n-----------------------------------------------\n" +
                   $"\n{"Source Files",StatisticsKeyWidth}" +
                   $"\n{"- Total",StatisticsKeyWidth}{CreateFileStatistic(FixedFiles.Concat(UnmatchedFiles).ToList())}" +
                   $"\n{"- Matched",StatisticsKeyWidth}{CreateFileStatistic(FixedFiles)}" +
                   $"\n{"  - Merged",StatisticsKeyWidth - 2}{CreateFileStatistic(FixedFiles.Where(x => x.Merged))}" +
                   $"\n{"  - Ignored",StatisticsKeyWidth - 2}{CreateFileStatistic(FixedFiles.Where(x => x.Ignored))}" +
                   $"\n{"  - Skipped",StatisticsKeyWidth - 2}{CreateFileStatistic(FixedFiles.Where(x => x.Skipped))}" +
                   $"\n{"- Unmatched",StatisticsKeyWidth}{CreateFileStatistic(UnmatchedFiles)}" +
                   "\n  (Unknown & Unsupported)" +
                   "\n" +
                   $"\n{"Time Taken",StatisticsKeyWidth}{ElapsedTime.TotalSeconds:f2}s";
        }

        private const double WindowMargin = 0;
    }
}