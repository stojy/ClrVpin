using System;
using System.Windows;
using ClrVpin.Controls;

namespace ClrVpin.Importer
{
    public sealed class ImporterStatisticsViewModel
    {
        public ImporterStatisticsViewModel(TimeSpan elapsedTime)
        {
            ElapsedTime = elapsedTime;
        }

        internal TimeSpan ElapsedTime { get; }
        public Window Window { get; set; }

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
        }

        public void Close() => Window.Close();
        private const int WindowMargin = 0;


        //protected override string CreateTotalStatistics()
        //{
        //    return "\n-----------------------------------------------\n" +
        //           $"\n{"Source Files",StatisticsKeyWidth}" +
        //           $"\n{"- Total",StatisticsKeyWidth}{CreateFileStatistic(GameFiles.Concat(UnmatchedFiles).ToList())}" +
        //           $"\n{"- Matched",StatisticsKeyWidth}{CreateFileStatistic(GameFiles)}" +
        //           $"\n{"  - Merged",StatisticsKeyWidth - 2}{CreateFileStatistic(GameFiles.Where(x=> x.Merged))}" +
        //           $"\n{"  - Ignored",StatisticsKeyWidth - 2}{CreateFileStatistic(GameFiles.Where(x => x.Ignored))}" +
        //           $"\n{"  - Skipped",StatisticsKeyWidth - 2}{CreateFileStatistic(GameFiles.Where(x => x.Skipped))}" +
        //           $"\n{"- Unmatched",StatisticsKeyWidth}{CreateFileStatistic(UnmatchedFiles)}" +
        //           "\n  (Unknown & Unsupported)" +
        //           "\n" +
        //           $"\n{"Time Taken",StatisticsKeyWidth}{ElapsedTime.TotalSeconds:f2}s";
        //}
    }
}