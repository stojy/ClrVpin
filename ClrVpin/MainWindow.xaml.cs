using System.Text;
using ClrVpin.Models.Settings;
using ClrVpin.Shared;
using MaterialDesignThemes.Wpf;
using PropertyChanged;

namespace ClrVpin
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

            Activated += (_, _) =>
            {
                Model.ScreenWorkArea = this.GetCurrentScreenWorkArea();

                if (SettingsManager.WasReset && !_configWasResetHandled)
                {
                    _configWasResetHandled = true;
                    DialogHost.Show(new Message
                    {
                        Title = "New settings are available",
                        Detail = "Please review your settings."
                    }).ContinueWith(_ => Dispatcher.Invoke(() => new Settings.SettingsViewModel().Show(this)));
                }
            };
        }

        public Model Model { get; set; }
        private bool _configWasResetHandled;
    }
}