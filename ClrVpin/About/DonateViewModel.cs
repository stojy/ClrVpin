using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.About
{
    [AddINotifyPropertyChangedInterface]
    public class DonateViewModel
    {
        // donations setup via stoj@stoj.net
        private const string PaypalDonateUrl = @"https://www.paypal.com/donate/?business=HT4GWFWEDWDCJ&no_recurring=0&item_name=ClrVpin+open+source+project.++https%3A%2F%2Fgithub.com%2Fstojy%2FClrVpin&currency_code=AUD";

        public DonateViewModel()
        {
            NavigateToPayPalCommand = new ActionCommand(() => Process.Start(new ProcessStartInfo(PaypalDonateUrl) { UseShellExecute = true}));
        }

        public ICommand NavigateToPayPalCommand { get; }

        public void Show(Window parent)
        {
            var window = new MaterialWindowEx
            {
                Owner = parent,
                Content = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
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