using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ClrVpin.Controls;
using Microsoft.Xaml.Behaviors.Core;

namespace ClrVpin.Logging
{
    public class LoggingViewModel
    {
        public LoggingViewModel()
        {
            LogsView = new ListCollectionView(Logger.Logs);
            File = Logger.File;

            NavigateToFileCommand = new ActionCommand(NavigateToFile);
        }

        public ListCollectionView LogsView { get; }
        public string File { get; }
        public ICommand NavigateToFileCommand { get; }

        public void Show(Window parentWindow, double left, double top)
        {
            _window = new MaterialWindowEx
            {
                Owner = parentWindow,
                Title = "Logs",
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = left,
                Top = top,
                Width = Model.ScreenWorkArea.Width - left - 5,
                Height = Model.ScreenWorkArea.Height - top - 5,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("LoggingTemplate") as DataTemplate
            };
            _window.Show();
        }

        public void Close() => _window.Close();

        private void NavigateToFile() => Process.Start(new ProcessStartInfo(File) {UseShellExecute = true});
        
        private Window _window;
    }
}