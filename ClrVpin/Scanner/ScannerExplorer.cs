using System.Collections.ObjectModel;
using System.Windows;
using ClrVpin.Models;
using MaterialDesignExtensions.Controls;
using PropertyChanged;

namespace ClrVpin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class ScannerExplorer
    {
        public ScannerExplorer(ObservableCollection<Game> games)
        {
            Games = games;
        }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindow
            {
                Owner = parentWindow,
                Title = "Scanner Explorer",
                Left = left,
                Top = top,
                Width = Model.ScreenWorkArea.Width - left - 5,
                Height = (Model.ScreenWorkArea.Height - 10) / 3,
                MinWidth = 400,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ScannerExplorerTemplate") as DataTemplate
            };
            Window.Show();
        }

        public void Close() => Window.Close();

        public Window Window { get; private set; }
        public ObservableCollection<Game> Games { get; }
    }
}