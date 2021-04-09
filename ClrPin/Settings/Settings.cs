using System.Windows;
using PropertyChanged;

namespace ClrPin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class Settings
    {
        private readonly Window _mainWindow;

        public Settings(Window mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void Show()
        {
            var window = new Window
            {
                Content = Model.Settings,
                ContentTemplate = _mainWindow.FindResource("SettingsTemplate") as DataTemplate,
            };
            window.ShowDialog();
        }
    }
}