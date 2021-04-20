using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;
using ClrVpin.Models;
using NLog;
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

            CheckHitTypesView = new ListCollectionView(CreateCheckHitTypes().ToList());
            _fixHitTypes = CreateFixHitTypes();
            FixHitTypesView = new ListCollectionView(_fixHitTypes.ToList());
        }

        public ListCollectionView CheckHitTypesView { get; set; }
        public ListCollectionView FixHitTypesView { get; set; }

        public ActionCommand<string> ConfigureCheckContentTypesCommand { get; set; }
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

        private void ShowResults()
        {
            var scannerResults = new ScannerResults(_scannerWindow, Games);
            scannerResults.Show();

            var scannerStatistics = new ScannerStatistics(Games, _scanStopWatch, _unknownFiles);
            scannerStatistics.Show(_scannerWindow, scannerResults.Window);

            var explorerWindow = new ScannerExplorer(Games);
            explorerWindow.Show(_scannerWindow, scannerResults.Window);

            _scannerWindow.Hide();
            scannerResults.Window.Closed += (_, _) =>
            {
                scannerStatistics.Close();
                explorerWindow.Close();
                _scannerWindow.Show();
            };
        }

        private IEnumerable<FeatureType> CreateCheckHitTypes()
        {
            // show all hit types
            var contentTypes = Hit.Types.Select(hitType =>
            {
                var featureType = new FeatureType
                {
                    Description = hitType.GetDescription(),
                    IsSupported = true,
                    IsActive = true
                };

                featureType.SelectedCommand = new ActionCommand(() =>
                {
                    Config.CheckHitTypes.Toggle(hitType);

                    // toggle the fix hit type checked & enabled
                    var fixHitType = _fixHitTypes.First(x => x.Description == featureType.Description);
                    fixHitType.IsSupported = featureType.IsActive;
                    if (!featureType.IsActive)
                        fixHitType.IsActive = false;
                });

                return featureType;
            });

            return contentTypes.ToList();
        }

        private static IEnumerable<FeatureType> CreateFixHitTypes()
        {
            // show all hit types, but allow them to be enabled and selected indirectly via the check hit type
            var contentTypes = Hit.Types.Select(hitType => new FeatureType
            {
                Description = hitType.GetDescription(),
                IsSupported = true,
                IsActive = true,
                SelectedCommand = new ActionCommand(() => Config.FixHitTypes.Toggle(hitType))
            });

            return contentTypes.ToList();
        }

        private static void ConfigureCheckContentTypes(string contentType) => Config.CheckContentTypes.Toggle(contentType);

        private void Start()
        {
            // todo; show progress bar

            _scanStopWatch = Stopwatch.StartNew();

            var games = GetDatabase();

            // todo; retrieve 'missing games' from spreadsheet
            
            Check(games);
            
            Fix(games);

            Games = new ObservableCollection<Game>(games);

            _scanStopWatch.Stop();

            ShowResults();
        }

        private void Check(List<Game> games)
        {
            // for the configured content types only.. check the installed content files against those specified in the database
            var checkContentTypes = Content.SupportedTypes.Where(type => Config.CheckContentTypes.Contains(type.Type));
            checkContentTypes.ForEach(contentSetup =>
            {
                var mediaFiles = GetMedia(contentSetup);
                var unknownMedia = AddMedia(games, mediaFiles, contentSetup.GetContentHits);

                // todo; add non-media content, e.g. tables and b2s

                _unknownFiles.AddRange(unknownMedia);
            });

            CheckMissing(games);
        }

        private static void Fix(List<Game> games)
        {
            games.ForEach(game =>
            {
                game.Content.ContentHitsCollection.ForEach(contentHitCollection =>
                {
                    if (contentHitCollection.Hits.Any(hit => hit.Type == HitType.Valid))
                    {
                        // all hit files can be deleted :)
                        contentHitCollection.Hits.Where(hit => hit.Type != HitType.Valid).ForEach(hit =>
                        {
                            switch (hit.Type)
                            {
                                case HitType.DuplicateExtension:
                                case HitType.Fuzzy:
                                case HitType.TableName:
                                case HitType.WrongCase:
                                    Delete(hit);
                                    break;
                            }
                        });
                    }
                });
            });
        }

        private static void Delete(Hit hit)
        {
            if (Config.FixHitTypes.Contains(hit.Type))
                Logger.Info($"deleting: type={hit.Type}, content={hit.ContentType}, path={hit.Path}");
        }

        private static void CheckMissing(List<Game> games)
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

                if ((matchedGame = games.FirstOrDefault(game =>
                    string.Equals(game.Description, Path.GetFileNameWithoutExtension(mediaFile), StringComparison.CurrentCultureIgnoreCase))) != null)
                    getContentHits(matchedGame).Add(HitType.WrongCase, mediaFile);
                else if ((matchedGame = games.FirstOrDefault(game => game.TableFile == Path.GetFileNameWithoutExtension(mediaFile))) != null)
                    getContentHits(matchedGame).Add(HitType.TableName, mediaFile);
                else
                    unknownMediaFiles.Add(new Tuple<string, long>(mediaFile, new FileInfo(mediaFile).Length));
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

        private readonly IEnumerable<FeatureType> _fixHitTypes;
        private readonly MainWindow _parentWindow;
        private Window _scannerWindow;
        private Stopwatch _scanStopWatch;
        private readonly List<Tuple<string, long>> _unknownFiles = new List<Tuple<string, long>>();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    }
}