using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
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
            ThanksCommand = new ActionCommand(() => new ThanksViewModel().Show(_window));
            DonateCommand = new ActionCommand(() => new DonateViewModel().Show(_window));

            var version = Assembly.GetEntryAssembly()?.GetName().Version!;
            AssemblyVersion = $"v{version?.Major}.{version?.Minor}.{version?.Build}.{version?.Revision}";
        }

        public string AssemblyVersion { get; set; }

        public ICommand SourceCommand { get; set; }
        public ICommand AuthorCommand { get; set; }
        public ICommand HelpCommand { get; set; }
        public ICommand ThanksCommand { get; set; }
        public ICommand DonateCommand { get; set; }

        public void Show(Window parent)
        {
            _window = new MaterialWindowEx
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

            _window.Show();
            parent.Hide();
            _window.Closed += (_, _) => parent.Show();
        }

        private MaterialWindowEx _window;

        private const string GitHubRepoUrl = @"https://github.com/stojy/ClrVpin";
        private const string GitHubAuthorUrl = @"https://github.com/stojy";
        private const string GitHubHelpUrl = @"https://github.com/stojy/ClrVpin/wiki/How-To-Use";
    }
}