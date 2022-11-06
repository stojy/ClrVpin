using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Home;
using ClrVpin.Scanner;
using PropertyChanged;
using Utils;

namespace ClrVpin.About
{
    [AddINotifyPropertyChangedInterface]
    public class AboutViewModel : IShowViewModel
    {
        public AboutViewModel()
        {
            SourceCommand = new ActionCommand(() => Process.Start(new ProcessStartInfo(GitHubRepoUrl) { UseShellExecute = true }));
            AuthorCommand = new ActionCommand(() => Process.Start(new ProcessStartInfo(GitHubAuthorUrl) { UseShellExecute = true }));
            HelpCommand = new ActionCommand(() => Process.Start(new ProcessStartInfo(GitHubHelpUrl) { UseShellExecute = true }));
            ThanksCommand = new ActionCommand(() => new ThanksViewModel().Show(_window));
            UpdateCommand = new ActionCommand(CheckAndHandleUpdate);
            DonateCommand = new ActionCommand(() => new DonateViewModel().Show(_window));

            AssemblyVersion = $"v{VersionManagement.GetProductVersion()}";
        }

        public string AssemblyVersion { get; }

        public ICommand SourceCommand { get; }
        public ICommand AuthorCommand { get; }
        public ICommand HelpCommand { get; }
        public ICommand ThanksCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DonateCommand { get; }

        public Window Show(Window parent)
        {
            _window = new MaterialWindowEx
            {
                Owner = parent,
                Content = this,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                SizeToContent = SizeToContent.WidthAndHeight,
                Resources = parent.Resources,
                ContentTemplate = parent.FindResource("AboutTemplate") as DataTemplate,
                ResizeMode = ResizeMode.NoResize,
                Title = "About"
            };

            _window.Show();

            return _window;
        }

        public Action<bool> ProgressChanged { get; set; }

        private async void CheckAndHandleUpdate()
        {
            await VersionManagementService.CheckAndHandle(_window, true);
        }

        private MaterialWindowEx _window;

        private const string GitHubRepoUrl = @"https://github.com/stojy/ClrVpin";
        private const string GitHubAuthorUrl = @"https://github.com/stojy";
        private const string GitHubHelpUrl = @"https://github.com/stojy/ClrVpin/wiki/How-To-Use";
    }
}