using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;
using ByteSizeLib;
using ClrPin.Models;
using ClrPin.Settings;
using PropertyChanged;
using Utils;

namespace ClrPin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class Scanner
    {
        public Scanner(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            StartCommand = new ActionCommand(Start);
            ExpandGamesCommand = new ActionCommandParam<bool>(ExpandItems);
            SearchTextCommand = new ActionCommand(SearchTextChanged);

            FilterHitTypeCommand = new ActionCommandParam<HitType>(FilterHitType);
            FilterMediaTypeCommand = new ActionCommandParam<string>(FilterMediaType);
            Start();
        }

        private const int StatisticsKeyWidth = -30;
        private readonly List<HitType> _filteredHitTypes = new List<HitType>(Hit.Types);
        private readonly List<string> _filteredMediaTypes = new List<string>(Media.Types);

        private readonly MainWindow _mainWindow;
        private DispatcherTimer _searchTextChangedDelayTimer;
        private Stopwatch _scanStopWatch;

        public ActionCommandParam<bool> ExpandGamesCommand { get; set; }
        public ActionCommandParam<string> FilterMediaTypeCommand { get; set; }
        public ActionCommandParam<HitType> FilterHitTypeCommand { get; set; }

        public ObservableCollection<Game> Games { get; set; }
        public ICommand StartCommand { get; set; }
        public ListCollectionView SmellyGamesView { get; set; }
        public ObservableCollection<Game> SmellyGames { get; set; }

        public string SearchText { get; set; } = "";

        public ICommand SearchTextCommand { get; set; }

        public string Statistics { get; set; }

        private void FilterMediaType(string mediaType)
        {
            if (_filteredMediaTypes.Contains(mediaType))
                _filteredMediaTypes.Remove(mediaType);
            else
                _filteredMediaTypes.Add(mediaType);

            Games.ForEach(game => game.Media.SmellyHitsView.Refresh());
            InitSmellyGamesView();
        }

        private void FilterHitType(HitType hitType)
        {
            if (_filteredHitTypes.Contains(hitType))
                _filteredHitTypes.Remove(hitType);
            else
                _filteredHitTypes.Add(hitType);

            Games.ForEach(game => game.Media.SmellyHitsView.Refresh());
            InitSmellyGamesView();
        }


        private void ExpandItems(bool expand)
        {
            SmellyGames.ForEach(game => game.IsExpanded = expand);
            SmellyGamesView.Refresh();
        }

        private void SearchTextChanged()
        {
            // delay processing text changed
            if (_searchTextChangedDelayTimer == null)
            {
                _searchTextChangedDelayTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(300)};
                _searchTextChangedDelayTimer.Tick += (_, _) =>
                {
                    SmellyGamesView.Refresh();
                    _searchTextChangedDelayTimer.Stop();
                };
            }

            _searchTextChangedDelayTimer.Stop(); // Resets the timer
            _searchTextChangedDelayTimer.Start();
        }

        public void Show()
        {
            var resultsWindow = new Window
            {
                Owner = _mainWindow,
                Title = "Scanner Results",
                Left = _mainWindow.Left,
                Top = _mainWindow.Top + _mainWindow.Height + 5,
                SizeToContent = SizeToContent.Width,
                MinWidth = 400,
                Height = 500,
                Content = this,
                ContentTemplate = _mainWindow.FindResource("ScannerResultsTemplate") as DataTemplate
            };
            resultsWindow.Show();

            var statisticsWindow = new Window
            {
                Owner = _mainWindow,
                Title = "Scanner Statistics",
                Left = _mainWindow.Left,
                Top = _mainWindow.Top + _mainWindow.Height + resultsWindow.Height + 10,
                SizeToContent = SizeToContent.Width,
                MinWidth = 400,
                Height = 650,
                Content = this,
                ContentTemplate = _mainWindow.FindResource("ScannerStatisticsTemplate") as DataTemplate
            };
            statisticsWindow.Show();

            var explorerWindow = new Window
            {
                Owner = _mainWindow,
                Title = "Scanner Explorer",
                Left = resultsWindow.Left + resultsWindow.Width + 5,
                Top = _mainWindow.Top,
                SizeToContent = SizeToContent.Height,
                MinHeight = 500,
                MaxHeight = 1000,
                MinWidth = 400,
                Content = this,
                ContentTemplate = _mainWindow.FindResource("ScannerExplorerTemplate") as DataTemplate
            };
            explorerWindow.Show();
        }

        private void Start()
        {
            _scanStopWatch = Stopwatch.StartNew();

            var games = GetDatabase();

            // todo; retrieve 'missing games' from spreadsheet

            // check the installed media files against those that are registered in the database
            var unknownFiles = new List<string>();
            Media.SupportedTypes.ForEach(mediaSetup =>
            {
                var mediaFiles = GetMedia(mediaSetup);
                var unknownMedia = AddMedia(games, mediaFiles, mediaSetup.GetMediaHits);

                unknownFiles.AddRange(unknownMedia);
            });

            Update(games);

            Games = new ObservableCollection<Game>(games);

            InitSmellyGamesView();

            _scanStopWatch.Stop();
            
            CreateStatistics(unknownFiles);
        }

        private void CreateStatistics(ICollection unknownFiles)
        {
            Statistics =
                $"{CreateHitTypeStatistics()}\n" +
                $"{CreateTotalStatistics(unknownFiles)}";
        }

        private string CreateHitTypeStatistics()
        {
            // for every hit type, create stats against every media type
            var hitStatistics = Hit.Types.Select(hitType =>
            {
                var title = $"{hitType.GetDescription()}";

                var contents = string.Join("\n",
                    Media.Types.Select(type =>
                        $"- {type,StatisticsKeyWidth + 2}{SmellyGames.Count(g => g.Media.MediaHitsCollection.First(x => x.Type == type).Hits.Any(hit => hit.Type == hitType))}/{Games.Count}"));
                return $"{title}\n{contents}";
            });

            return $"{string.Join("\n\n", hitStatistics)}";
        }

        private string CreateTotalStatistics(ICollection unknownFiles)
        {
            var validHits = Games.SelectMany(x => x.Media.MediaHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitType.Valid).ToList();

            return "\n-----------------------------------------------\n" +
                   $"\n{"Total Games",StatisticsKeyWidth}{Games.Count}" +
                   $"\n{"Unneeded Files",StatisticsKeyWidth}{unknownFiles.Count}" +
                   $"\n{"Valid Files",StatisticsKeyWidth}{validHits.Count}/{Games.Count * Media.Types.Length} ({(decimal) validHits.Count / (Games.Count * Media.Types.Length):P2})" +
                   $"\n{"Valid Files Size",StatisticsKeyWidth}{ByteSize.FromBytes(validHits.Sum(x => x.Size)).ToString("#")}" + 
                   $"\n\n{"Time Taken",StatisticsKeyWidth}{_scanStopWatch.Elapsed.TotalSeconds:f2}s";
        }

        private void InitSmellyGamesView()
        {
            SmellyGames = new ObservableCollection<Game>(Games.Where(game => game.Media.SmellyHitsView.Count > 0));
            SmellyGamesView = new ListCollectionView(SmellyGames);

            // filter at games level.. NOT filter at media type or game hit type
            SmellyGamesView.Filter += gameObject =>
            {
                if (SearchText.Length == 0)
                    return true;
                return ((Game) gameObject).Description.ToLower().Contains(SearchText.ToLower());
            };
        }

        private void Update(List<Game> games)
        {
            games.ForEach(game =>
            {
                // add missing media
                game.Media.MediaHitsCollection.ForEach(mediaHitCollection =>
                {
                    if (!mediaHitCollection.Hits.Any(hit => hit.Type == HitType.Valid || hit.Type == HitType.WrongCase))
                        mediaHitCollection.Add(HitType.Missing, game.Description);
                });

                game.Media.Update(() => _filteredMediaTypes, () => _filteredHitTypes);
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

        private static List<string> GetMedia(MediaType mediaType)
        {
            var files = mediaType.Extensions.Select(ext => Directory.GetFiles(mediaType.QualifiedFolder, ext));

            return files.SelectMany(x => x).ToList();
        }
    }
}