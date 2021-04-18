using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using ClrVpin.Models;
using PropertyChanged;
using Utils;

namespace ClrVpin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class ScannerResults
    {
        public ScannerResults(Window parentWindow, ObservableCollection<Game> games)
        {
            Games = games;
            _parentWindow = parentWindow;

            SearchTextCommand = new ActionCommand(SearchTextChanged);
            ExpandGamesCommand = new ActionCommand<bool>(ExpandItems);

            FilterContentTypeCommand = new ActionCommand<string>(FilterContentType);
            FilterHitTypeCommand = new ActionCommand<HitType>(FilterHitType);
        }

        public ObservableCollection<Game> Games { get; set; }
        public ListCollectionView SmellyGamesView { get; set; }
        public ObservableCollection<Game> SmellyGames { get; set; }
        public List<string> FilteredContentTypesView { get; set; }
        public ActionCommand<bool> ExpandGamesCommand { get; set; }
        public ActionCommand<string> FilterContentTypeCommand { get; set; }
        public ActionCommand<HitType> FilterHitTypeCommand { get; set; }
        public string SearchText { get; set; } = "";
        public ICommand SearchTextCommand { get; set; }

        public Window Window { get; set; }

        public List<FilteredContentType> FilteredContentTypes { get; set; }

        public void Show()
        {
            FilteredContentTypes = CreateFilteredContentTypes();

            Update(Games);

            InitSmellyGamesView();

            Window = new Window
            {
                Owner = _parentWindow,
                Title = "Scanner Results",
                Left = 10,
                Top = 10,
                SizeToContent = SizeToContent.Width,
                MinWidth = 500,
                Height = 500,
                Content = this,

                // todo; load resources in a centralised location
                ContentTemplate = _parentWindow.Owner.FindResource("ScannerResultsTemplate") as DataTemplate
            };
            Window.Show();
        }

        private static List<FilteredContentType> CreateFilteredContentTypes()
        {
            // filtered results content types based on the scanner configuration
            var filteredContentTypes = Config.CheckContentTypes.Select(x => new FilteredContentType
            {
                Description = x
            });

            return filteredContentTypes.ToList();
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

        private void Update(IEnumerable<Game> games)
        {
            games.ForEach(game =>
            {
                // update smelly status
                game.Content.Update(() => Config.CheckContentTypes, () => _filteredHitTypes);
            });
        }

        private void FilterContentType(string contentType)
        {
            // _filteredContentTypes.Toggle(contentType);
            Games.ForEach(game => game.Content.SmellyHitsView.Refresh());
            
            // todo; don't re-init the entire list!  use similar to search logic
            InitSmellyGamesView();
        }

        private void FilterHitType(HitType hitType)
        {
            _filteredHitTypes.Toggle(hitType);
            Games.ForEach(game => game.Content.SmellyHitsView.Refresh());
            InitSmellyGamesView();
        }

        private void ExpandItems(bool expand)
        {
            SmellyGames.ForEach(game => game.IsExpanded = expand);
            SmellyGamesView.Refresh();
        }

        private void InitSmellyGamesView()
        {
            SmellyGames = new ObservableCollection<Game>(Games.Where(game => game.Content.SmellyHitsView.Count > 0));
            SmellyGamesView = new ListCollectionView(SmellyGames);

            // filter at games level.. NOT filter at content type or game hit type
            SmellyGamesView.Filter += gameObject =>
            {
                if (SearchText.Length == 0)
                    return true;
                return ((Game) gameObject).Description.ToLower().Contains(SearchText.ToLower());
            };
        }

        private readonly List<HitType> _filteredHitTypes = new List<HitType>(Hit.Types);

        private readonly Window _parentWindow;
        private DispatcherTimer _searchTextChangedDelayTimer;
    }
}