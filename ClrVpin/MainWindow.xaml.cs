using System.Text;
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
                if (Model.Config.WasReset)
                {
                    DialogHost.Show(new Message
                    {
                        Title = "New settings are available",
                        Detail = "Please review your settings."
                    }).ContinueWith(_ => Dispatcher.Invoke(() => new Settings.Settings().Show(this)));
                }
            };
        }

        public Model Model { get; set; }
    }
}