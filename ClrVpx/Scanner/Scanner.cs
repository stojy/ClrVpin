using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using ByteSizeLib;
using ClrVpx.Models;
using ClrVpx.Settings;
using PropertyChanged;
using Utils;

namespace ClrVpx.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class Scanner
    {
        public const string MediaLaunchAudio = "Launch Audio";
        public const string MediaTableAudio = "Table Audio";
        public const string MediaTableVideos = "Table Videos";
        public const string MediaBackglassVideos = "Backglass Videos";
        public const string MediaWheelImages = "Wheel Images";

        private readonly MainWindow _mainWindow;

        private readonly List<MediaSetup> _supportedMedia = new List<MediaSetup>
        {
            new MediaSetup {Folder = MediaTableAudio, Extensions = new[] {"*.mp3", "*.wav"}},
            new MediaSetup {Folder = MediaLaunchAudio, Extensions = new[] {"*.mp3", "*.wav"}},
            new MediaSetup {Folder = MediaTableVideos, Extensions = new[] {"*.mp4", "*.f4v"}},
            new MediaSetup {Folder = MediaBackglassVideos, Extensions = new[] {"*.mp4", "*.f4v"}},
            new MediaSetup {Folder = MediaWheelImages, Extensions = new[] {"*.png"}}
            //new MediaSetup {Folder = "Tables", Extensions = new[] {"*.png"}, GetHits = g => g.WheelImageHits},
            //new MediaSetup {Folder = "Backglass", Extensions = new[] {"*.png"}, GetHits = g => g.WheelImageHits},
            //new MediaSetup {Folder = "Point of View", Extensions = new[] {"*.png"}, GetHits = g => g.WheelImageHits},
        };

        public Scanner(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            StartCommand = new ActionCommand(Start);
            Start();
        }


        public ObservableCollection<Game> Games { get; set; }
        public ICommand StartCommand { get; set; }

        public ObservableCollection<Game> DirtyGames { get; set; }

        public void Show()
        {
            var window = new Window
            {
                Content = this,
                ContentTemplate = _mainWindow.FindResource("ScannerExplorerTemplate") as DataTemplate
            };
            window.ShowDialog();
        }

        private void Start()
        {
            var games = GetDatabase();

            // check the installed media files against those that are registered in the database
            // todo; check against spreadsheet for 'missing games'
            var orphans = new List<string>();
            _supportedMedia.ForEach(fileSetup =>
            {
                var media = GetMedia(fileSetup);
                orphans.AddRange(UpdateGames(games, media, fileSetup.GetHits));
            });

            Games = new ObservableCollection<Game>(games);

            Games.ForEach(game => game.Dirty = game.Media.Any(media => media.Value.Count != 1 || media.Value.Any(m => m.Score != 100)));

            DirtyGames = new ObservableCollection<Game>(games.Where(g => g.Dirty));
        }

        private IEnumerable<string> UpdateGames(List<Game> games, IEnumerable<string> mediaFiles, Func<Game, ObservableCollection<Hit>> getHits)
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
                else if ((matchedGame = games.FirstOrDefault(game => game.TableFile == Path.GetFileNameWithoutExtension(mediaFile))) != null)
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
                {
                    orphanedFiles.Add(mediaFile);
                }
            });

            return orphanedFiles;
        }

        private static Hit CreateHit(string path, int score)
        {
            return new Hit
            {
                Path = path,
                File = Path.GetFileName(path),
                Size = ByteSize.FromBytes(new FileInfo(path).Length).ToString("#"),
                Score = score
            };
        }

        private static List<Game> GetDatabase()
        {
            var file = $@"{SettingsModel.VpxFrontendFolder}\Databases\Visual Pinball\Visual Pinball.xml";
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

        private IEnumerable<string> GetMedia(MediaSetup mediaSetup)
        {
            // todo; store file details, e.g. sixe
            var files = mediaSetup.Extensions.Select(ext => Directory.GetFiles(mediaSetup.Path, ext));

            return files.SelectMany(x => x);
        }
    }
}