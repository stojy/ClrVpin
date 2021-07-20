using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using MaterialDesignExtensions.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.About
{
    [AddINotifyPropertyChangedInterface]
    public class AboutViewModel
    {
        public AboutViewModel()
        {
            NavigateToGitHubRepoCommand = new ActionCommand(NavigateToGitHubRepo);
            NavigateToGitHubAuthorCommand = new ActionCommand(NavigateToGitHubAuthor);

            var version = Assembly.GetEntryAssembly()?.GetName().Version!;
            AssemblyVersion = $"v{version?.Major}.{version?.Minor}.{version?.Build}";
        }

        public string GitHubRepoUrl { get; set; } = @"https://github.com/stojy/ClrVpin";
        public string GitHubAuthorUrl { get; set; } = @"https://github.com/stojy";
        public string AssemblyVersion { get; set; }

        public ICommand NavigateToGitHubRepoCommand { get; set; }
        public ICommand NavigateToGitHubAuthorCommand { get; set; }

        public void Show(Window parent)
        {
            var window = new MaterialWindow
            {
                Owner = parent,
                Content = this,
                //SizeToContent = SizeToContent.WidthAndHeight,
                Width = 520,
                Height = 630,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Resources = parent.Resources,
                ContentTemplate = parent.FindResource("AboutTemplate") as DataTemplate,
                ResizeMode = ResizeMode.NoResize,
                Title = "About"
            };

            window.Show();
            parent.Hide();
            window.Closed += (_, _) => parent.Show();
        }

        private void NavigateToGitHubRepo() => Process.Start(new ProcessStartInfo(GitHubRepoUrl) {UseShellExecute = true});
        private void NavigateToGitHubAuthor() => Process.Start(new ProcessStartInfo(GitHubAuthorUrl) {UseShellExecute = true});
    }
}