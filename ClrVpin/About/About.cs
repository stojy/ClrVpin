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
            NavigateToGitHubRepoCommand = new ActionCommand(NavigateToGitHubRepo);
            NavigateToGitHubAuthorCommand = new ActionCommand(NavigateToGitHubAuthor);
        }

        public string GitHubRepoUrl { get; set; } = @"https://github.com/stojy/ClrVpin";
        public string GitHubAuthorUrl { get; set; } = @"https://github.com/stojy";

        public ICommand NavigateToGitHubRepoCommand { get; set; }
        public ICommand NavigateToGitHubAuthorCommand { get; set; }

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

        private void NavigateToGitHubRepo() => Process.Start(new ProcessStartInfo(GitHubRepoUrl) {UseShellExecute = true});
        private void NavigateToGitHubAuthor() => Process.Start(new ProcessStartInfo(GitHubAuthorUrl) {UseShellExecute = true});
    }
}