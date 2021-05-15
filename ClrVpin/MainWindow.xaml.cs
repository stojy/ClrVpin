using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
                if (!Model.Config.IsReviewRequired)
                {
                    //Dispatcher.Invoke(() => DialogHost.Show("Hello"));
                    //DialogHost.Show(this).Wait();
                    //MessageBox.Show(this, "Please check the updated settings", "Settings have been updated", MessageBoxButton.OK, MessageBoxImage.Information);
                    //new Settings.Settings().Show(this);
                }
            };
        }

        public Model Model { get; set; }

        private void DialogHost_OnDialogClosing(object sender, DialogClosingEventArgs eventargs)
        {
            new Settings.Settings().Show(this);
        }
    }
}