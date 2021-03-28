using System;
using System.Collections.ObjectModel;
using System.Windows;
using PropertyChanged;

namespace ClrVpx
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = this;
         
            InitializeComponent();

            Scanner = new ScannerModel();
            Scanner.Results = new ObservableCollection<User>();
            Scanner.Results.Add(new User { Id = 1, Name = "John Doe", Birthday = new DateTime(1971, 7, 23) });
            Scanner.Results.Add(new User { Id = 2, Name = "Jane Doe", Birthday = new DateTime(1974, 1, 17) });
            Scanner.Results.Add(new User { Id = 3, Name = "Sammy Doe", Birthday = new DateTime(1991, 9, 2) });

        }

        public ScannerModel Scanner { get; set; }
    }

    public class ScannerModel
    {
        public ObservableCollection<User> Results { get; set; }
    }

    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime Birthday { get; set; }
    }
}