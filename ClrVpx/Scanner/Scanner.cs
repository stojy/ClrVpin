using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using System.Xml.Linq;
using ClrVpx.Models;
using PropertyChanged;
using Utils;

namespace ClrVpx.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class Scanner
    {
        public Scanner()
        {
            // initialise encoding to workaround the error "Windows -1252 is not supported encoding name"
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Scan = new ActionCommand(StartScan);
            StartScan();
        }

        public ObservableCollection<Game> Results { get; set; }

        public ICommand Scan { get; set; }

        private void StartScan()
        {
            var fileName = @"C:\vp\apps\PinballX\Databases\Visual Pinball\Visual Pinball.xml";
            var doc = XDocument.Load(fileName);
            if (doc.Root == null)
                throw new Exception("Failed to load database");

            var menu = doc.Root.Deserialize<Menu>();
            var number = 1;
            menu.Games.ForEach(g =>
            {
                g.Number = number++;
                g.Ipdb = g.IpdbId ?? g.IpdbNr;
            });
            
            Results = new ObservableCollection<Game>(menu.Games);
        }
    }
}