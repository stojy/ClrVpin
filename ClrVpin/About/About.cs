using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using PropertyChanged;
using Utils;

namespace ClrVpin.About
{
    [AddINotifyPropertyChangedInterface]
    public class About
    {
        public About()
        {
            NavigateToGitHubCommand = new ActionCommand(NavigateToGitHub);
        }

        public string GitHubUrl { get; set; } = @"https://github.com/stojy/ClrVpin";

        public ICommand NavigateToGitHubCommand { get; set; }

        public void Show(Window parentWindow)
        {
            var window = new Window
            {
                Owner = parentWindow,
                Content = this,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("AboutTemplate") as DataTemplate,
                ResizeMode = ResizeMode.NoResize
            };

            window.Show();
            parentWindow.Hide();
            window.Closed += (_, _) => parentWindow.Show();
        }

        private void NavigateToGitHub()
        {
            Process.Start(new ProcessStartInfo(GitHubUrl) {UseShellExecute = true});
        }
    }
}