using System.Windows;
using System.Windows.Input;
using PropertyChanged;
using Utils;

namespace ClrVpin.Rebuilder
{
    [AddINotifyPropertyChangedInterface]
    public class Rebuilder
    {
        private readonly MainWindow _mainWindow;

        public Rebuilder(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            StartCommand = new ActionCommand(Start);
        }

        private void Start()
        {
        }

        public ICommand StartCommand { get; set; }

        public void Show()
        {
            var window = new Window
            {
                Content = this,
                ContentTemplate = _mainWindow.FindResource("RebuilderTemplate") as DataTemplate
            };
            window.ShowDialog();
        }
    }
}