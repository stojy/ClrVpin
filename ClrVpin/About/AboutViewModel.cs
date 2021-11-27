using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Donate;
using PropertyChanged;
using Utils;

namespace ClrVpin.About
{
    [AddINotifyPropertyChangedInterface]
    public class AboutViewModel
    {
        public AboutViewModel()
        {
            SourceCommand = new ActionCommand(() => Process.Start(new ProcessStartInfo(GitHubRepoUrl) { UseShellExecute = true }));
            AuthorCommand = new ActionCommand(() => Process.Start(new ProcessStartInfo(GitHubAuthorUrl) { UseShellExecute = true }));
            HelpCommand = new ActionCommand(() => Process.Start(new ProcessStartInfo(GitHubHelpUrl) { UseShellExecute = true }));
            DonateCommand = new ActionCommand(() => new DonateViewModel().Show(_mainWindow));

            var version = Assembly.GetEntryAssembly()?.GetName().Version!;
            AssemblyVersion = $"v{version?.Major}.{version?.Minor}.{version?.Build}.{version?.Revision}";
        }

        public string GitHubRepoUrl { get; set; } = @"https://github.com/stojy/ClrVpin";
        public string GitHubAuthorUrl { get; set; } = @"https://github.com/stojy/ClrVpin/issues/new/choose";
        public string GitHubHelpUrl { get; set; } = @"https://github.com/stojy/ClrVpin/wiki/How-To-Use";

        public string AssemblyVersion { get; set; }

        public ICommand SourceCommand { get; set; }
        public ICommand AuthorCommand { get; set; }
        public ICommand HelpCommand { get; set; }
        public ICommand DonateCommand { get; set; }

        public void Show(Window parent)
        {
            _mainWindow = parent;

            var window = new MaterialWindowEx
            {
                Owner = parent,
                Content = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                Resources = parent.Resources,
                ContentTemplate = parent.FindResource("AboutTemplate") as DataTemplate,
                ResizeMode = ResizeMode.NoResize,
                Title = "About"
            };

            window.Show();
            parent.Hide();
            window.Closed += (_, _) => parent.Show();
        }

        private Window _mainWindow;
    }
}