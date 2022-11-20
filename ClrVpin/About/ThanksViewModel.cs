using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.About
{
    [AddINotifyPropertyChangedInterface]
    public class ThanksViewModel
    {
        public ThanksViewModel()
        {
            NavigateToPage = new ActionCommand<string>(url => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }));
        }

        public ICommand NavigateToPage { get; }

        public List<Contributor> Contributors { get; } = new()
        {
            new Contributor("https://virtual-pinball-spreadsheet.web.app", "VPS Web App: an excellent web page rendering of the traditional google sheets spreadsheet (beta)"),
            new Contributor("https://www.facebook.com/VPSheet", "VPS: maintainers of the google sheets spreadsheet (now a json file) used as the source of truth for the Feeder feature"),
            new Contributor("http://mjrnet.org/pinscape/PinballY.php", "PinballY: an amazing table frontend. Not just fast and flexible.. it's fully open source :)"),
            new Contributor("https://www.vpforums.org", "VP Forums: forum and download repository for everything VP. Be sure to join and contribute."),
            new Contributor("https://vpuniverse.com", "VP Universe: forum and download repository for everything VP. Be sure to join and contribute."),
            new Contributor("https://vpdb.io", "vpdb: download repository for everything VP."),
        };

        public void Show(MaterialWindowEx parent)
        {
            var window = new MaterialWindowEx
            {
                Owner = parent,
                Content = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                Resources = parent.Resources,
                ContentTemplate = parent.FindResource("ThanksTemplate") as DataTemplate,
                ResizeMode = ResizeMode.NoResize,
                Title = "Thanks"
            };

            window.Show();
            parent.Hide();

            window.Closed += (_, _) => parent.TryShow();
        }
    }
}