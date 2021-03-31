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

        class MediaSetup
        {
            public string Title { get; init; }
            public string[] Extensions { get; init; }
            public Func<Game, ObservableCollection<Hit>> GetHits { get; init; }
        }

        private readonly List<MediaSetup> _mediaSetups = new List<MediaSetup>
        {
            new MediaSetup {Title = "Table Audio", Extensions = new[] {"*.mp3", "*.wav"}, GetHits = g => g.TableAudioHits},
            new MediaSetup {Title = "Launch Audio", Extensions = new[] {"*.mp3", "*.wav"}, GetHits = g => g.LaunchAudioHits},
            new MediaSetup {Title = "Table Videos", Extensions = new[] {"*.mp4", "*.f4v"}, GetHits = g => g.TableVideoHits},
            new MediaSetup {Title = "Backglass Videos", Extensions = new[] {"*.mp4", "*.f4v"}, GetHits = g => g.BackglassVideoHits},
            new MediaSetup {Title = "Wheel Images", Extensions = new[] {"*.png"}, GetHits = g => g.WheelImageHits}
        };

        private void StartScan()
        {
            var games = GetDatabase();

            // todo; check PBX media folder and new folder(s)

            _mediaSetups.ForEach(m =>
            {
                var media = GetMedia(m.Title, m.Extensions);
                var orphans = Merge(games, media, m.GetHits);
                
                Console.WriteLine(orphans);
            });

            Results = new ObservableCollection<Game>(games);
        }

        private IEnumerable<string> Merge(List<Game> games, IEnumerable<string> mediaFiles, Func<Game, ObservableCollection<Hit>> getHits)
        {
            var orphanedFiles = new List<string>();

            mediaFiles.ForEach(mediaFile =>
            {
                Game matchedGame;
                Hit hit = null;

                // check for hit
                // todo; rebuilder; score result for existing vs new files
                // fuzzy match media to the table
                // todo; lexical word, etc
                if ((matchedGame = games.FirstOrDefault(game => game.Description == Path.GetFileNameWithoutExtension(mediaFile))) != null)
                    hit = CreateHit(mediaFile, 100);
                else if ((matchedGame = games.FirstOrDefault(game => game.Name == Path.GetFileNameWithoutExtension(mediaFile))) != null)
                    hit = CreateHit(mediaFile, 99);
                
                // add hit
                if (hit != null)
                {
                    // add
                    var hits = getHits(matchedGame);
                    hits.Add(hit);

                    // sort
                    var orderedHits = hits.OrderByDescending(h => h.Score).ToList();
                    hits.Clear();
                    orderedHits.ForEach(o => hits.Add(o));
                }
                else
                    orphanedFiles.Add(mediaFile);
            });

            return orphanedFiles;
        }

        private static Hit CreateHit(string mediaFile, int score)
        {
            return new Hit
            {
                File = mediaFile,
                Score = score
            };
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