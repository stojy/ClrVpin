using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
            var games = GetDatabase();

            var tableAudio = GetMedia("Table Audio", new [] { "*.mp3", "*.wav" });

            Results = new ObservableCollection<Game>(games);
        }

        private static List<Game> GetDatabase()
        {
            var file = $@"{Settings.Settings.VpxFrontendFolder}\Databases\Visual Pinball\Visual Pinball.xml";
            var doc = XDocument.Load(file);
            if (doc.Root == null)
                throw new Exception("Failed to load database");

            var menu = doc.Root.Deserialize<Menu>();
            var number = 1;
            menu.Games.ForEach(g =>
            {
                g.Number = number++;
                g.Ipdb = g.IpdbId ?? g.IpdbNr;
            });

            return menu.Games;
        }

        
        private IEnumerable<string> GetMedia(string folder, string[] extensions)
        {
            var path = $@"{Settings.Settings.VpxFrontendFolder}\Media\Visual Pinball\{folder}";

            var files = extensions.Select(ext => Directory.GetFiles(path, ext));

            return files.SelectMany(x => x);
        }
    }
}