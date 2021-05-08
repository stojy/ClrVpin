using System.Windows;
using PropertyChanged;

namespace ClrVpin.About
{
    [AddINotifyPropertyChangedInterface]
    public class About
    {
        public void Show(Window parentWindow)
        {
            var window = new Window
            {
                Owner = parentWindow,
                Content = this,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("AboutTemplate") as DataTemplate
            };

            window.Show();
            parentWindow.Hide();
            window.Closed += (_, _) => parentWindow.Show();
        }
    }
}