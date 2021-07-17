using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ClrVpin.Models;
using ClrVpin.Shared;
using MaterialDesignExtensions.Controls;

namespace ClrVpin.Scanner
{
    public class ScannerStatistics : Statistics
    {
        public ScannerStatistics(ObservableCollection<Game> games, TimeSpan elapsedTime, ICollection<FileDetail> gameFiles, ICollection<FileDetail> unknownFiles)
            : base(games, elapsedTime, gameFiles, unknownFiles)
        {
            // hit type stats for all supported types only
            // - including the extra 'under the hood' types.. valid, unknown, unsupported
            SupportedHitTypes = Config.HitTypes.ToList();


            SupportedContentTypes = Config.ContentTypes.Where(x => Model.Config.SelectedCheckContentTypes.Contains(x.Description)).ToList();

            SelectedCheckContentTypes = Model.Config.SelectedCheckContentTypes;

            // rebuilder doesn't support check and fix separately
            SelectedCheckHitTypes = Model.Config.SelectedCheckHitTypes.ToList();
            SelectedFixHitTypes = Model.Config.SelectedFixHitTypes.ToList();

            // unlike rebuilder, the total count represents the number of Games
            TotalCount = Games.Count;
        }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindow
            {
                Owner = parentWindow,
                Title = "Scanner Statistics",
                Left = left,
                Top = top,
                Width = 770,
                Height = Model.ScreenWorkArea.Height - 10,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ScannerStatisticsTemplate") as DataTemplate
            };
            Window.Show();

            CreateStatistics();
        }

        protected override string CreateTotalStatistics()
        {
            var validHits = Games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitTypeEnum.Valid).ToList();

            var eligibleHits = Games.Count * Model.Config.SelectedCheckContentTypes.Count;

            // all files
            var allFilesCount = validHits.Count + GameFiles.Count;
            var allFilesSize = validHits.Sum(x => x.Size) + GameFiles.Sum(x => x.Size) ?? 0;

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
                   $"\n{"- Available Tables",StatisticsKeyWidth}{Games.Count}" +
                   $"\n{"- Possible Content",StatisticsKeyWidth}{Games.Count * Config.ContentTypes.Length}" +
                   $"\n{"- Checked Content",StatisticsKeyWidth}{eligibleHits}" +
                   $"\n\n{"All Files",StatisticsKeyWidth}{CreateFileStatistic(allFilesCount, allFilesSize)}" +
                   $"\n\n{"Valid Files",StatisticsKeyWidth}{CreateFileStatistic(validHits.Count, validHits.Sum(x => x.Size ?? 0))}" +
                   $"\n{"- Collection",StatisticsKeyWidth}{validHits.Count}/{eligibleHits} ({(decimal) validHits.Count / eligibleHits:P2})" +
                   $"\n\n{"Fixed/Fixable Files",StatisticsKeyWidth}{CreateFileStatistic(GameFiles.Count, GameFiles.Sum(x => x.Size))}" +
                   $"\n{"- renamed",StatisticsKeyWidth}{CreateFileStatistic(fixFilesRenamed.Count, fixFilesRenamedSize)}" +
                   $"\n{"- removed",StatisticsKeyWidth}{CreateFileStatistic(fixFilesDeleted.Count, fixFilesDeletedSize)}" +
                   $"\n{"  (criteria: unknown)",StatisticsKeyWidth}{CreateFileStatistic(fixFilesDeletedUnknown.Count, fixFilesDeletedUnknownSize)}" +
                   $"\n{"- renamable and removable",StatisticsKeyWidth}{CreateFileStatistic(fixFilesIgnored.Count, fixFilesIgnoredSize)}" +
                   $"\n{"  (criteria: unknown)",StatisticsKeyWidth}{CreateFileStatistic(fixFilesIgnoredUnknown.Count, fixFilesIgnoredUnknownSize)}" +
                   $"\n\n{"Time Taken",StatisticsKeyWidth}{ElapsedTime.TotalSeconds:f2}s";
        }
    }
}