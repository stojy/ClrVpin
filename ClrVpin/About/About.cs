using System.Windows;
using PropertyChanged;

namespace ClrVpin.About
{
    [AddINotifyPropertyChangedInterface]
    public class About
    {
        public void Show(Window parent)
        {
            var window = new Window
            {
                Owner = parent,
                Content = this,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ContentTemplate = parent.FindResource("AboutTemplate") as DataTemplate
            };

            window.Show();
            parent.Hide();
            window.Closed += (_, _) => parent.Show();
        }
    }
}