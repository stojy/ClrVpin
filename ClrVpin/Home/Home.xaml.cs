using System;
using System.Diagnostics;
using System.Text;
using ClrVpin.Logging;
using ClrVpin.Shared;
using MaterialDesignThemes.Wpf;
using Octokit;
using PropertyChanged;
using Utils;
using Application = System.Windows.Application;

namespace ClrVpin.Home
{
    [AddINotifyPropertyChangedInterface]
    // ReSharper disable once UnusedMember.Global
    public partial class MainWindow
    {
        public MainWindow()
        {
            // initialise encoding to workaround the error "Windows -1252 is not supported encoding name"
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            DataContext = new Model(this);

            InitializeComponent();

            Activated += async (_, _) =>
            {
                Model.ScreenWorkArea = this.GetCurrentScreenWorkArea();

                // guard against multiple window activations
                if (Model.SettingsManager.WasReset && !_wasConfigResetHandled)
                {
                    _wasConfigResetHandled = true;
                    await DialogHost.Show(new RestartInfo
                    {
                        Title = "Your settings have been reset",
                        Detail = "ClrVpin will now be restarted."
                    }, "HomeDialog").ContinueWith(_ => Dispatcher.Invoke(Restart));
                }
            };

            Loaded += async (_, _) =>
            {
                var settings = Model.SettingsManager.Settings;
                var shouldCheckForUpdate = settings.EnableCheckForNewVersion &&
                                           (settings.LastCheckForNewVersion == null || DateTime.Now - settings.LastCheckForNewVersion.Value > TimeSpan.FromDays(1));
                Logger.Info($"Version checking: shouldCheckForUpdate={shouldCheckForUpdate}, EnableCheckForNewVersion={settings.EnableCheckForNewVersion}, LastCheckForNewVersion={settings.LastCheckForNewVersion}");

                if (shouldCheckForUpdate)
                {
                    var release = await VersionManagement.Check("stojy", "ClrVpin", msg => Logger.Info($"Version checking: {msg}"));
                    if (release != null)
                    {
                        var result = await DialogHost.Show(new VersionUpdateInfo
                        {
                            Title = "A new version is available",
                            ExistingVersion = VersionManagement.GetProductVersion(),
                            NewVersion = release.TagName,
                            CreatedAt = release.CreatedAt.LocalDateTime,
                            ReleaseNotes = release.Body,
                            ViewNewVersionCommand = new ActionCommand(() => ViewNewVersion(release))
                        }, "HomeDialog") as VersionManagementAction?;

                        await VersionManagement.Process(release, result);
                    }

                    // update last check AFTER processing to ensure the msi installer (if invoked) doesn't update the version (since it causes the process to exit before it reaches here)
                    // - intention is that the new version when it starts up will perform another version check to ensure everything is up to date (which it should be!)
                    settings.LastCheckForNewVersion = DateTime.Now;
                    Model.SettingsManager.Write();
                }
            };
        }

        private static async void ViewNewVersion(Release release)
        {
            await VersionManagement.Process(release, VersionManagementAction.View);
        }

        private static void Restart()
        {
            Process.Start(Process.GetCurrentProcess().MainModule!.FileName!);
            Application.Current.Shutdown();
        }

        private bool _wasConfigResetHandled;
    }
}