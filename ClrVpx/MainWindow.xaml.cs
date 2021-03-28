using PropertyChanged;

namespace ClrVpx
{
    [AddINotifyPropertyChangedInterface]
    // ReSharper disable once UnusedMember.Global
    public partial class MainWindow
    {
        public MainWindow()
        {
            Model = new Model();
            DataContext = Model;
            
            InitializeComponent();
        }

        public Model Model { get; set; }
    }
}