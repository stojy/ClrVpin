using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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

            Model = new Model(this);
            DataContext = Model;

            InitializeComponent();

            Activated += async (_, _) =>
            {
                Model.ScreenWorkArea = this.GetCurrentScreenWorkArea();

                if (Model.SettingsManager.WasReset && !_configWasResetHandled)
                {
                    _configWasResetHandled = true;
                    await DialogHost.Show(new RestartInfo
                    {
                        Title = "Your settings have been reset",
                        Detail = "ClrVpin will now be restarted."
                    }, "HomeDialog").ContinueWith(_ => Dispatcher.Invoke(Restart));
                }

                if (!_skipCheckForUpdate)
                {
                    _skipCheckForUpdate = true;
                    var release = await VersionManagement.CheckForUpdate("stojy", "ClrVpin");

                    if (release != null)
                    {
                        var result = await DialogHost.Show(new VersionUpdateInfo
                        {
                            Title = "A new version is available",
                            ExistingVersion = VersionManagement.GetProductVersion(),
                            NewVersion = release.TagName,
                            CreatedAt = release.CreatedAt.LocalDateTime,
                            ReleaseNotes = release.Body,
                        }, "HomeDialog") as VersionManagementAction?;

                        await VersionManagement.ProcessAction(release, result);
                    }
                }
            };
        }

        private static void Restart()
        {
            Process.Start(Process.GetCurrentProcess().MainModule!.FileName!);
            Application.Current.Shutdown();
        }

        private Model Model { get; }
        private bool _configWasResetHandled;
        private bool _skipCheckForUpdate;
    }
}