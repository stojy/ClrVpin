using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
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

        private readonly List<MediaSetup> _mediaSetups = new List<MediaSetup>
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

        public void Show()
        {
            var explorerWindow = new Window
            {
                Owner = _mainWindow,
                Content = this,
                ContentTemplate = _mainWindow.FindResource("ScannerExplorerTemplate") as DataTemplate
            };
            explorerWindow.Show();

            var resultsWindow = new Window
            {
                Owner = _mainWindow,
                Content = this, 
                ContentTemplate = _mainWindow.FindResource("ScannerResultsTemplate") as DataTemplate
            };
            resultsWindow.Show();
        }

        private void Start()
        {
            var games = GetDatabase();

            // todo; retrieve 'missing games' from spreadsheet

            // add media file info
            games.ForEach(game => game.Media.Init(game));

            // check the installed media files against those that are registered in the database
            var unknownMediaFiles = new List<string>();
            _mediaSetups.ForEach(mediaSetup =>
            {
                var mediaFiles = GetMedia(mediaSetup);
                var unknownMedia = AddMedia(games, mediaFiles, mediaSetup.GetHits);
                unknownMediaFiles.AddRange(unknownMedia);
            });

            Games = new ObservableCollection<Game>(games);
        }

        private IEnumerable<string> AddMedia(IReadOnlyCollection<Game> games, IEnumerable<string> mediaFiles, Func<Game, MediaHits> getHits)
        {
            var unknownMediaFiles = new List<string>();

            mediaFiles.ForEach(mediaFile =>
            {
                Game matchedGame;

                // check for hit.. only 1 hit per file, so order is important!
                // todo; fuzzy match.. e.g. partial matches, etc.
                if ((matchedGame = games.FirstOrDefault(game => game.Description == Path.GetFileNameWithoutExtension(mediaFile))) != null)
                    getHits(matchedGame).Add(HitType.Valid, mediaFile);
                else if ((matchedGame = games.FirstOrDefault(game => game.TableFile == Path.GetFileNameWithoutExtension(mediaFile))) != null)
                    getHits(matchedGame).Add(HitType.TableName, mediaFile);
                else
                    unknownMediaFiles.Add(mediaFile);
            });

            return unknownMediaFiles;
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
            var files = mediaSetup.Extensions.Select(ext => Directory.GetFiles(mediaSetup.Path, ext));

            return files.SelectMany(x => x);
        }
    }
}