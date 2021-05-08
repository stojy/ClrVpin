using System.Text;
using System.Windows;
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
                if (Model.Config.IsReviewRequired)
                {
                    MessageBox.Show(this, "Please check the updated settings", "Settings have been updated", MessageBoxButton.OK, MessageBoxImage.Information);
                    new Settings.Settings().Show(this);
                }
            };
        }

        public Model Model { get; set; }
    }
}