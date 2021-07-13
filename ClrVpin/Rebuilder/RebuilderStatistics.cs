using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ClrVpin.Models;
using ClrVpin.Shared;
using MaterialDesignExtensions.Controls;

namespace ClrVpin.Rebuilder
{
    public sealed class RebuilderStatistics : Statistics
    {
        public RebuilderStatistics(ObservableCollection<Game> games, TimeSpan elapsedTime, ICollection<FileDetail> gameFiles, ICollection<FileDetail> unknownFiles)
            : base(games, elapsedTime, gameFiles, unknownFiles)
        {
            // hit type stats for all supported types only
            // - including the extra 'under the hood' types.. valid, unknown, unsupported
            SupportedHitTypes = Config.MatchTypes.ToList();

            // content type stats for the single content type being rebuilt
            SupportedContentTypes = new List<ContentType> {Config.GetDestinationContentType()};

            // rebuilder only supports a single selected content type
            SelectedCheckContentTypes = new List<string> {Model.Config.DestinationContentType};

            // rebuilder doesn't support check and fix separately
            SelectedCheckHitTypes = Model.Config.SelectedMatchTypes.ToList();
            SelectedFixHitTypes = SelectedCheckHitTypes;

            // unlike scanner, the total count represents the number of files that were analyzed
            TotalCount = GameFiles.Count + UnknownFiles.Count;

            IsRemoveUnknownSupported = false;

            FixedTerm = "merged";
            FixableTerm = "mergeable";
        }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindow
            {
                Owner = parentWindow,
                Title = "Rebuilder Statistics",
                Left = left,
                Top = top,
                Width = 600,
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
            var allFilesCount = GameFiles.Count + UnknownFiles.Count;
            var allFilesSize = GameFiles.Sum(x => x.Size) + UnknownFiles.Sum(x => x.Size);

            return "\n-----------------------------------------------\n" +
                   $"\n{"Source Files",StatisticsKeyWidth}{CreateFileStatistic(allFilesCount, allFilesSize)}" +
                   $"\n\n{"Time Taken",StatisticsKeyWidth}{ElapsedTime.TotalSeconds:f2}s";
        }
    }
}