using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using MaterialDesignExtensions.Controls;
using Microsoft.Xaml.Behaviors.Core;

namespace ClrVpin.Logging
{
    public class Logging
    {
        public Logging()
        {
            LogsView = new ListCollectionView(Logger.Logs);
            File = Logger.File;

            NavigateToFileCommand = new ActionCommand(NavigateToFile);
        }

        public ListCollectionView LogsView { get; }
        public string File { get; }
        public ICommand NavigateToFileCommand { get; }

        public void Show(Window parentWindow, double left, double top, int height)
        {
            _window = new MaterialWindow
            {
                Owner = parentWindow,
                Title = "Logs",
                WindowStartupLocation = WindowStartupLocation.Manual,
                Height = height,
                Width = 1790,
                Content = this,
                Left = left,
                Top = top,
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