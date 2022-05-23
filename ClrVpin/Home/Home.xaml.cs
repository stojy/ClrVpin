using System.Diagnostics;
using System.Text;
using ClrVpin.Shared;
using MaterialDesignThemes.Wpf;
using PropertyChanged;
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
                if (VersionManagementView.ShouldCheck()) 
                    await VersionManagementView.CheckAndHandle();
            };
        }

        private static void Restart()
        {
            Process.Start(Process.GetCurrentProcess().MainModule!.FileName!);
            Application.Current.Shutdown();
        }

        private bool _wasConfigResetHandled;
    }
}