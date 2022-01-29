using System;
using System.Windows;
using ClrVpin.Controls;
using ClrVpin.Shared;
using PropertyChanged;

namespace ClrVpin.Importer
{
    [AddINotifyPropertyChangedInterface]
    public class ImporterStatisticsViewModel
    {
        public ImporterStatisticsViewModel(TimeSpan elapsedTime)
        {
            _elapsedTime = elapsedTime;
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


        private string CreateTotalStatistics() => "\n-----------------------------------------------\n" +
                                                  //$"\n{"Source Files",StatisticsKeyWidth}" +
                                                  //$"\n{"- Total",StatisticsKeyWidth}{CreateFileStatistic(GameFiles.Concat(UnmatchedFiles).ToList())}" +
                                                  //$"\n{"- Matched",StatisticsKeyWidth}{CreateFileStatistic(GameFiles)}" +
                                                  //$"\n{"  - Merged",StatisticsKeyWidth - 2}{CreateFileStatistic(GameFiles.Where(x => x.Merged))}" +
                                                  //$"\n{"  - Ignored",StatisticsKeyWidth - 2}{CreateFileStatistic(GameFiles.Where(x => x.Ignored))}" +
                                                  //$"\n{"  - Skipped",StatisticsKeyWidth - 2}{CreateFileStatistic(GameFiles.Where(x => x.Skipped))}" +
                                                  //$"\n{"- Unmatched",StatisticsKeyWidth}{CreateFileStatistic(UnmatchedFiles)}" +
                                                  //"\n  (Unknown & Unsupported)" +
                                                  //"\n" +
                                                  $"\n{"Time Taken",StatisticsViewModel.StatisticsKeyWidth}{_elapsedTime.TotalSeconds:f2}s";

        private readonly TimeSpan _elapsedTime;
        private const int WindowMargin = 0;
    }
}