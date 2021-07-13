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
            HitTypes = Config.MatchTypes.ToList();

            // content type stats for the single content type being rebuilt
            ContentTypes = new List<ContentType> {Config.GetDestinationContentType()};

            // rebuilder only supports a single selected content type
            SelectedCheckContentTypes = new List<string> {Model.Config.DestinationContentType};

            // rebuilder doesn't support check and fix separately
            //SelectedCheckHitTypes = Model.Config.SelectedMatchTypes.ToList();
            SelectedCheckHitTypes = Model.Config.SelectedMatchTypes.ToList();
            SelectedFixHitTypes = SelectedCheckHitTypes;

            // unlike scanner, the total count represents the number of files that were analyzed
            TotalCount = GameFiles.Count + UnknownFiles.Count;

            IsRemoveSupported = false;
        }

        protected override string FixedTerm { get; } = "merged";
        protected override string FixableTerm { get; } = "mergeable";

        protected override int TotalCount { get; }

        public override bool IsRemoveSupported { get; }

        protected override IList<HitTypeEnum> SelectedFixHitTypes { get; set; }
        protected override IList<HitTypeEnum> SelectedCheckHitTypes { get; set; }

        protected override IList<HitType> HitTypes { get; set; }
        protected override IList<ContentType> ContentTypes { get; set; }
        protected override IList<string> SelectedCheckContentTypes { get; set; }


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
            var validHits = Games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitTypeEnum.Valid).ToList();

            var eligibleHits = Games.Count * Model.Config.SelectedCheckContentTypes.Count;

            // all files
            var allFilesCount = GameFiles.Count + UnknownFiles.Count;
            var allFilesSize = GameFiles.Sum(x => x.Size) + UnknownFiles.Sum(x => x.Size);

            // renamed
            // - must be configured as a fix hit type
            // - unknown is n/a apply for renamable, i.e. since we don't know what game/table to rename it to
            var fixFilesRenamed = GameFiles.Where(x => x.Renamed).ToList();
            var fixFilesRenamedSize = fixFilesRenamed.Sum(x => x.Size);

            // removed (deleted)
            // - must be configured as a fix hit type
            var fixFilesDeleted = GameFiles.Where(x => x.Deleted).ToList();
            var fixFilesDeletedSize = fixFilesDeleted.Sum(x => x.Size);
            var fixFilesDeletedUnknown = fixFilesDeleted.Where(x => x.HitType == HitTypeEnum.Unknown).ToList();
            var fixFilesDeletedUnknownSize = fixFilesDeletedUnknown.Sum(x => x.Size);

            // ignored (removable and renamable)
            // - includes renamable AND removable files
            // - unknown..
            //   - n/a apply for renamable, i.e. since we don't know what game/table to rename it to
            //   - applicable for removable
            var fixFilesIgnored = GameFiles.Where(x => x.Ignored).ToList();
            var fixFilesIgnoredSize = fixFilesIgnored.Sum(x => x.Size);
            var fixFilesIgnoredUnknown = fixFilesIgnored.Where(x => x.HitType == HitTypeEnum.Unknown).ToList();
            var fixFilesIgnoredUnknownSize = fixFilesIgnoredUnknown.Sum(x => x.Size);

            return "\n-----------------------------------------------\n" +
                   "\nTotals" +
                   $"\n{"- Available Files",StatisticsKeyWidth}{allFilesCount}" +
                   $"\n{"- Possible Content",StatisticsKeyWidth}{Games.Count * Config.ContentTypes.Length}" +
                   $"\n{"- Checked Content",StatisticsKeyWidth}{eligibleHits}" +

                   $"\n\n{"All Files",StatisticsKeyWidth}{CreateFileStatistic(allFilesCount, allFilesSize)}" +
                   $"\n\n{"Valid Files",StatisticsKeyWidth}{CreateFileStatistic(validHits.Count, validHits.Sum(x => x.Size ?? 0))}" +
                   $"\n{"- Collection",StatisticsKeyWidth}{validHits.Count}/{eligibleHits} ({(decimal) validHits.Count / eligibleHits:P2})" +
                   //$"\n\n{"Fixed/Fixable Files",StatisticsKeyWidth}{CreateFileStatistic(fixFileDetails.Count, fixFileDetails.Sum(x => x.Size))}" +
                   $"\n{"- renamed",StatisticsKeyWidth}{CreateFileStatistic(fixFilesRenamed.Count, fixFilesRenamedSize)}" +
                   $"\n{"- removed",StatisticsKeyWidth}{CreateFileStatistic(fixFilesDeleted.Count, fixFilesDeletedSize)}" +
                   $"\n{"  (criteria: unknown)",StatisticsKeyWidth}{CreateFileStatistic(fixFilesDeletedUnknown.Count, fixFilesDeletedUnknownSize)}" +
                   $"\n{"- renamable and removable",StatisticsKeyWidth}{CreateFileStatistic(fixFilesIgnored.Count, fixFilesIgnoredSize)}" +
                   $"\n{"  (criteria: unknown)",StatisticsKeyWidth}{CreateFileStatistic(fixFilesIgnoredUnknown.Count, fixFilesIgnoredUnknownSize)}" +
                   $"\n\n{"Time Taken",StatisticsKeyWidth}{ElapsedTime.TotalSeconds:f2}s";
        }
    }
}