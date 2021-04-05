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
        public Scanner(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            StartCommand = new ActionCommand(Start);
            Start();
        }

        private readonly MainWindow _mainWindow;

        public ObservableCollection<Game> Games { get; set; }
        public ICommand StartCommand { get; set; }

        public ObservableCollection<Game> SmellyGames { get; set; }

        public void Show()
        {
            var resultsWindow = new Window
            {
                Owner = _mainWindow,
                Left = _mainWindow.Left,
                Top = _mainWindow.Top + _mainWindow.Height + 10,
                SizeToContent = SizeToContent.Width,
                MinWidth = 400,
                Height = 500,
                Content = this,
                ContentTemplate = _mainWindow.FindResource("ScannerResultsTemplate") as DataTemplate
            };
            resultsWindow.Show();

            var explorerWindow = new Window
            {
                Owner = _mainWindow,
                Left = resultsWindow.Left + resultsWindow.Width + 10,
                Top = _mainWindow.Top,
                SizeToContent = SizeToContent.Height,
                MinHeight = 499,
                MaxHeight = 1000,
                MinWidth = 400,
                Content = this,
                ContentTemplate = _mainWindow.FindResource("ScannerExplorerTemplate") as DataTemplate
            };
            explorerWindow.Show();
        }

        private void Start()
        {
            var games = GetDatabase();

            // todo; retrieve 'missing games' from spreadsheet

            // check the installed media files against those that are registered in the database
            var unknownMediaFiles = new List<string>();
            Media.SupportedTypes.ForEach(mediaSetup =>
            {
                var mediaFiles = GetMedia(mediaSetup);
                var unknownMedia = AddMedia(games, mediaFiles, mediaSetup.GetMediaHits);
                unknownMediaFiles.AddRange(unknownMedia);
            });

            AddMissingMedia(games);
            Games = new ObservableCollection<Game>(games);
            SmellyGames = new ObservableCollection<Game>(games.Where(game => game.Media.IsSmelly));
        }

        private static void AddMissingMedia(List<Game> games)
        {
            games.ForEach(game =>
            {
                game.Media.MediaHitsCollection.ForEach(mediaHitCollection =>
                {
                    if (!mediaHitCollection.Hits.Any(hit => hit.Type == HitType.Valid || hit.Type == HitType.WrongCase))
                        mediaHitCollection.Add(HitType.Missing, game.Description);
                });
            });
        }

        private IEnumerable<string> AddMedia(IReadOnlyCollection<Game> games, IEnumerable<string> mediaFiles, Func<Game, MediaHits> getMediaHits)
        {
            var unknownMediaFiles = new List<string>();

            mediaFiles.ForEach(mediaFile =>
            {
                Game matchedGame;

                // check for hit.. only 1 hit per file, so order is important!
                // todo; fuzzy match.. e.g. partial matches, etc.
                if ((matchedGame = games.FirstOrDefault(game => game.Description == Path.GetFileNameWithoutExtension(mediaFile))) != null)
                {
                    // if a match already exists, then assume this match is a duplicate name with wrong extension
                    var mediaHits = getMediaHits(matchedGame);
                    mediaHits.Add(mediaHits.Hits.Any(hit => hit.Type == HitType.Valid) ? HitType.DuplicateExtension : HitType.Valid, mediaFile);
                }
                else if ((matchedGame = games.FirstOrDefault(game => game.TableFile == Path.GetFileNameWithoutExtension(mediaFile))) != null)
                {
                    getMediaHits(matchedGame).Add(HitType.TableName, mediaFile);
                }
                else
                {
                    unknownMediaFiles.Add(mediaFile);
                }
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

        private IEnumerable<string> GetMedia(MediaType mediaType)
        {
            var files = mediaType.Extensions.Select(ext => Directory.GetFiles(mediaType.Path, ext));

            return files.SelectMany(x => x);
        }
    }
}