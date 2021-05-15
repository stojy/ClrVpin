using System.Windows;
using System.Windows.Input;
using PropertyChanged;
using Utils;

namespace ClrVpin.Rebuilder
{
    [AddINotifyPropertyChangedInterface]
    public class Rebuilder
    {
        public Rebuilder()
        {
            StartCommand = new ActionCommand(Start);
        }

        private void Start()
        {
        }

        public ICommand StartCommand { get; set; }

        public void Show(Window parentWindow)
        {
            var window = new Window
            {
                Owner = parentWindow,
                Content = this,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("RebuilderTemplate") as DataTemplate,
                ResizeMode = ResizeMode.NoResize
            };

            window.Show();
            parentWindow.Hide();
            window.Closed += (_, _) => parentWindow.Show();
        }
    }
}