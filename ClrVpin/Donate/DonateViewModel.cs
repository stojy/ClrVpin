using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using MaterialDesignExtensions.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.Donate
{
    [AddINotifyPropertyChangedInterface]
    public class DonateViewModel
    {
        private string _paypalDonateUrl = @"https://www.paypal.com/donate?business=PL536UKUXC852&no_recurring=0&currency_code=AUD";

        public DonateViewModel()
        {
            NavigateToPayPalCommand = new ActionCommand(() => Process.Start(new ProcessStartInfo(_paypalDonateUrl) { UseShellExecute = true}));
        }

        public ICommand NavigateToPayPalCommand { get; set; }

        public void Show(Window parent)
        {
            var window = new MaterialWindow
            {
                Owner = parent,
                Content = this,
                //SizeToContent = SizeToContent.WidthAndHeight,
                Width = 620,
                Height = 570,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Resources = parent.Resources,
                ContentTemplate = parent.FindResource("DonateTemplate") as DataTemplate,
                ResizeMode = ResizeMode.NoResize,
                Title = "Donate"
            };

            window.Show();
            parent.Hide();
            window.Closed += (_, _) => parent.Show();
        }
    }
}