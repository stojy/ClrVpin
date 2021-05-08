using System.Windows;
using System.Windows.Data;

namespace ClrVpin.Logging
{
    public class Logging
    {
        public Logging()
        {
            LogsView = new ListCollectionView(Logger.Logs);
        }

        public ListCollectionView LogsView { get; }

        public void Show(Window parentWindow, double left, double top, int height)
        {
            _window = new Window
            {
                Owner = parentWindow,
                Title = "Logs",
                WindowStartupLocation = WindowStartupLocation.Manual,
                Height = height,
                Width = 1200,
                Content = this,
                Left = left,
                Top = top,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("LoggingTemplate") as DataTemplate
            };
            _window.Show();
        }

        public void Close() => _window.Close();

        private Window _window;
    }
}