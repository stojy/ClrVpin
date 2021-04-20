using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using ClrVpin.Models;
using PropertyChanged;
using Utils;

namespace ClrVpin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class Scanner
    {
        public Scanner(MainWindow parentWindow)
        {
            _parentWindow = parentWindow;

            StartCommand = new ActionCommand(Start);

            ConfigureCheckContentTypesCommand = new ActionCommand<string>(ConfigureCheckContentTypes);
            ConfigureCheckHitTypesCommand = new ActionCommand<HitType>(ConfigureCheckHitTypes);
            ConfigureFixHitTypesCommand = new ActionCommand<HitType>(ConfigureFixHitTypes);
        }

        public ActionCommand<string> ConfigureCheckContentTypesCommand { get; set; }
        public ActionCommand<HitType> ConfigureCheckHitTypesCommand { get; set; }
        public ActionCommand<HitType> ConfigureFixHitTypesCommand { get; set; }
        public ObservableCollection<Game> Games { get; set; }
        public ICommand StartCommand { get; set; }

        public void Show()
        {
            _scannerWindow = new Window
            {
                Owner = _parentWindow,
                Title = "Scanner",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                Content = this,
                ContentTemplate = _parentWindow.FindResource("ScannerTemplate") as DataTemplate
            };
            _scannerWindow.Show();

            _parentWindow.Hide();
            _scannerWindow.Closed += (_, _) => _parentWindow.Show();
        }

        public void ShowResults()
        {
            var scannerResults = new ScannerResults(_scannerWindow, Games);
            scannerResults.Show();


            var scannerStatistics = new ScannerStatistics(Games, _scanStopWatch, _unknownFiles);
            scannerStatistics.Show(_scannerWindow, scannerResults.Window);

            var explorerWindow = new Window
            {
                Owner = _scannerWindow,
                Title = "Scanner Explorer",
                Left = scannerResults.Window.Left + scannerResults.Window.Width + 5,
                Top = scannerResults.Window.Top,
                SizeToContent = SizeToContent.Height,
                MinHeight = 500,
                MaxHeight = 1200,
                MinWidth = 400,
                Content = this,
                ContentTemplate = _parentWindow.FindResource("ScannerExplorerTemplate") as DataTemplate
            };
            explorerWindow.Show();

            _scannerWindow.Hide();
            scannerResults.Window.Closed += (_, _) =>
            {
                scannerStatistics.Close();
                explorerWindow.Close();
                _scannerWindow.Show();
            };
        }

        private static void ConfigureCheckContentTypes(string contentType) => Config.CheckContentTypes.Toggle(contentType);
        private static void ConfigureCheckHitTypes(HitType hitType) => Config.CheckHitTypes.Toggle(hitType);
        private static void ConfigureFixHitTypes(HitType hitType) => Config.FixHitTypes.Toggle(hitType);

        private void Start()
        {
            // todo; show progress bar

            _scanStopWatch = Stopwatch.StartNew();

            var games = GetDatabase();

            // todo; retrieve 'missing games' from spreadsheet

            _unknownFiles = new List<Tuple<string, long>>();
            
            // for the configured content types only.. check the installed content files against those specified in the database
            var checkContentTypes = Content.SupportedTypes.Where(type => Config.CheckContentTypes.Contains(type.Type));
            checkContentTypes.ForEach(contentSetup =>
            {
                var mediaFiles = GetMedia(contentSetup);
                var unknownMedia = AddMedia(games, mediaFiles, contentSetup.GetContentHits);

                // todo; add non-media content, e.g. tables and b2s
                
                _unknownFiles.AddRange(unknownMedia);
            });

            Update(games);

            Games = new ObservableCollection<Game>(games);

            _scanStopWatch.Stop();

            ShowResults();
        }

        private void Update(List<Game> games)
        {
            games.ForEach(game =>
            {
                // add missing content
                game.Content.ContentHitsCollection.ForEach(contentHitCollection =>
                {
                    if (!contentHitCollection.Hits.Any(hit => hit.Type == HitType.Valid || hit.Type == HitType.WrongCase))
                        contentHitCollection.Add(HitType.Missing, game.Description);
                });
            });
        }

        private static IEnumerable<Tuple<string, long>> AddMedia(IReadOnlyCollection<Game> games, IEnumerable<string> mediaFiles, Func<Game, ContentHits> getContentHits)
        {
            var unknownMediaFiles = new List<Tuple<string, long>>();

            mediaFiles.ForEach(mediaFile =>
            {
                Game matchedGame;

                // check for hit..
                // - skip hit types that aren't configured
                // - only 1 hit per file.. but a game can have multiple hits.. with a maximum of 1 valid hit
                // - todo; fuzzy match.. e.g. partial matches, etc.
                if ((matchedGame = games.FirstOrDefault(game => game.Description == Path.GetFileNameWithoutExtension(mediaFile))) != null)
                {
                    // if a match already exists, then assume this match is a duplicate name with wrong extension
                    // - file extension order is important as it determines the priority of the preferred extension
                    var contentHits = getContentHits(matchedGame);
                    contentHits.Add(contentHits.Hits.Any(hit => hit.Type == HitType.Valid) ? HitType.DuplicateExtension : HitType.Valid, mediaFile);
                }
                if ((matchedGame = games.FirstOrDefault(game => string.Equals(game.Description, Path.GetFileNameWithoutExtension(mediaFile), StringComparison.CurrentCultureIgnoreCase))) != null)
                {
                    getContentHits(matchedGame).Add(HitType.WrongCase, mediaFile);
                }
                else if ((matchedGame = games.FirstOrDefault(game => game.TableFile == Path.GetFileNameWithoutExtension(mediaFile))) != null)
                {
                    getContentHits(matchedGame).Add(HitType.TableName, mediaFile);
                }
                else
                {
                    unknownMediaFiles.Add(new Tuple<string, long>(mediaFile, new FileInfo(mediaFile).Length));
                }
            });

            return unknownMediaFiles;
        }

        private static List<Game> GetDatabase()
        {
            var file = $@"{Config.VpxFrontendFolder}\Databases\Visual Pinball\Visual Pinball.xml";
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

        private static IEnumerable<string> GetMedia(ContentType contentType)
        {
            var files = contentType.Extensions.Select(ext => Directory.GetFiles(contentType.QualifiedFolder, ext));

            return files.SelectMany(x => x).ToList();
        }

        private readonly MainWindow _parentWindow;
        private Window _scannerWindow;
        private Stopwatch _scanStopWatch;
        private List<Tuple<string, long>> _unknownFiles;
    }
}