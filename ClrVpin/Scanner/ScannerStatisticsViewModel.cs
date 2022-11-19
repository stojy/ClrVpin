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

namespace ClrVpin.Scanner
{
    public class ScannerStatisticsViewModel : StatisticsViewModel
    {
        public ScannerStatisticsViewModel(ObservableCollection<LocalGame> games, TimeSpan elapsedTime, ICollection<FileDetail> fixedFiles, ICollection<FileDetail> unmatchedFiles)
            : base(games, elapsedTime, fixedFiles, unmatchedFiles)
        {
            // hit type stats for all supported types only
            // - including the extra 'under the hood' types.. valid, unknown, unsupported
            SupportedHitTypes = StaticSettings.AllHitTypes.ToList();

            SupportedContentTypes = Settings.GetFixableContentTypes().Where(x => Settings.Scanner.SelectedCheckContentTypes.Contains(x.Description)).ToList();

            SelectedCheckContentTypes = Settings.Scanner.SelectedCheckContentTypes;

            // merger doesn't support check and fix separately
            SelectedCheckHitTypes = Settings.Scanner.SelectedCheckHitTypes.ToList();
            SelectedFixHitTypes = Settings.Scanner.SelectedFixHitTypes.ToList();

            // unlike merger, the total count represents the number of LocalGames
            TotalCount = Games.Count;
        }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindowEx
            {
                Owner = parentWindow,
                Title = "Scanner Statistics",
                Left = left,
                Top = top,
                Width = 770,
                Height = Model.ScreenWorkArea.Height - WindowMargin - WindowMargin,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ScannerStatisticsTemplate") as DataTemplate
            };
            Window.Show();

            CreateStatistics();
        }

        protected override string CreateTotalStatistics()
        {
            var validHits = Games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitTypeEnum.CorrectName).ToList();

            var eligibleHits = Games.Count * Settings.Scanner.SelectedCheckContentTypes.Count;

            // all files
            var allFilesCount = validHits.Count + FixedFiles.Count;
            var allFilesSize = validHits.Sum(x => x.Size) + FixedFiles.Sum(x => x.Size) ?? 0;

            // renamed
            // - must be configured as a fix hit type
            // - unknown is n/a apply for renamable, i.e. since we don't know what game/table to rename it to
            var fixedFilesRenamed = FixedFiles.Where(x => x.Renamed).ToList();
            var fixedFilesRenamedSize = fixedFilesRenamed.Sum(x => x.Size);

            // removed (deleted)
            // - must be configured as a fix hit type
            var fixedFilesDeleted = FixedFiles.Where(x => x.Deleted).ToList();
            var fixedFilesDeletedSize = fixedFilesDeleted.Sum(x => x.Size);
            var fixedFilesDeletedUnknown = fixedFilesDeleted.Where(x => x.HitType == HitTypeEnum.Unknown).ToList();
            var fixedFilesDeletedUnknownSize = fixedFilesDeletedUnknown.Sum(x => x.Size);

            // ignored (removable and renamable)
            // - includes renamable AND removable files
            // - unknown..
            //   - n/a apply for renamable, i.e. since we don't know what game/table to rename it to
            //   - applicable for removable
            var fixedFilesIgnored = FixedFiles.Where(x => x.Ignored).ToList();
            var fixedFilesIgnoredSize = fixedFilesIgnored.Sum(x => x.Size);
            var fixedFilesIgnoredUnknown = fixedFilesIgnored.Where(x => x.HitType == HitTypeEnum.Unknown).ToList();
            var fixedFilesIgnoredUnknownSize = fixedFilesIgnoredUnknown.Sum(x => x.Size);

            var eligibleHitsPercentage = eligibleHits == 0 ? "n/a" : $"{(decimal)validHits.Count / eligibleHits:P2}";
            var missingOrIncorrectHitsPercentage = eligibleHits == 0 ? "n/a" : $"{1 - (decimal)validHits.Count / eligibleHits:P2}";

            return "\n-----------------------------------------------\n" +
                   "\nTotals" +
                   $"\n{"- Available Tables",StatisticsKeyWidth}{Games.Count}" +
                   $"\n{"- Possible Content",StatisticsKeyWidth}{Games.Count * Settings.GetFixableContentTypes().Length}" +
                   $"\n{"- Checked Content",StatisticsKeyWidth}{eligibleHits}" +
                   $"\n\n{"All Files",StatisticsKeyWidth}{CreateFileStatistic(allFilesCount, allFilesSize)}" +
                   $"\n\n{"Correct Files",StatisticsKeyWidth}{CreateFileStatistic(validHits.Count, validHits.Sum(x => x.Size ?? 0))}" +
                   $"\n{"- Collection Present",StatisticsKeyWidth}{validHits.Count}/{eligibleHits} ({eligibleHitsPercentage})" +
                   $"\n{"- Collection Missing",StatisticsKeyWidth}{eligibleHits - validHits.Count}/{eligibleHits} ({missingOrIncorrectHitsPercentage})" +
                   $"\n\n{"Fixed/Fixable Files",StatisticsKeyWidth}{CreateFileStatistic(FixedFiles.Count, FixedFiles.Sum(x => x.Size))}" +
                   $"\n{"- renamed",StatisticsKeyWidth}{CreateFileStatistic(fixedFilesRenamed.Count, fixedFilesRenamedSize)}" +
                   $"\n{"- removed",StatisticsKeyWidth}{CreateFileStatistic(fixedFilesDeleted.Count, fixedFilesDeletedSize)}" +
                   $"\n{"  (criteria: unknown)",StatisticsKeyWidth}{CreateFileStatistic(fixedFilesDeletedUnknown.Count, fixedFilesDeletedUnknownSize)}" +
                   $"\n{"- renamable and removable",StatisticsKeyWidth}{CreateFileStatistic(fixedFilesIgnored.Count, fixedFilesIgnoredSize)}" +
                   $"\n{"  (criteria: unknown)",StatisticsKeyWidth}{CreateFileStatistic(fixedFilesIgnoredUnknown.Count, fixedFilesIgnoredUnknownSize)}" +
                   $"\n\n{"Time Taken",StatisticsKeyWidth}{ElapsedTime.TotalSeconds:f2}s";
        }

        private const double WindowMargin = 0;
    }
}