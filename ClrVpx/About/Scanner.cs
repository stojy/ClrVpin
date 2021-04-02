using System.Windows;
using PropertyChanged;

namespace ClrVpx.About
{
    [AddINotifyPropertyChangedInterface]
    public class About
    {
        private readonly MainWindow _mainWindow;

        public About(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void Show()
        {
            var window = new Window
            {
                Content = this,
                ContentTemplate = _mainWindow.FindResource("AboutTemplate") as DataTemplate
            };
            window.ShowDialog();
        }
    }
}