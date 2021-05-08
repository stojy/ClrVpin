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
        public const int Width = 530;
        public const int Height = 500;

        public ScannerResults(ObservableCollection<Game> games)
        {
            Games = games;

            SearchTextCommand = new ActionCommand(SearchTextChanged);
            ExpandGamesCommand = new ActionCommand<bool>(ExpandItems);

            _filteredContentTypes = CreateFilteredContentTypes();
            FilteredContentTypesView = new ListCollectionView(_filteredContentTypes.ToList());

            _filteredHitTypes = CreateFilteredHitTypes();
            FilteredHitTypesView = new ListCollectionView(_filteredHitTypes.ToList());

            UpdateSmellyStatus(Games);
            InitSmellyGamesView();
        }

        public ObservableCollection<Game> Games { get; set; }

        public ListCollectionView FilteredContentTypesView { get; set; }
        public ListCollectionView FilteredHitTypesView { get; set; }
        public ListCollectionView SmellyGamesView { get; set; }
        public ObservableCollection<Game> SmellyGames { get; set; }

        public ActionCommand<bool> ExpandGamesCommand { get; set; }
        public string SearchText { get; set; } = "";
        public ICommand SearchTextCommand { get; set; }

        public Window Window { get; set; }

        public void Show(Window parentWindow, int left, int top)
        {
            Window = new Window
            {
                Owner = parentWindow,
                Title = "Scanner Results",
                Left = left,
                Top = top,
                Width = Width,
                Height = Height,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ScannerResultsTemplate") as DataTemplate
            };
            Window.Show();
        }

        private IEnumerable<FeatureType> CreateFilteredHitTypes()
        {
            // show all hit types, but assign enabled and active based on the scanner configuration
            var filteredContentTypes = Config.HitTypes.Select(hitType => new FeatureType
            {
                Description = hitType.Description,
                IsSupported = Model.Config.CheckHitTypes.Contains(hitType.Enum),
                IsActive = Model.Config.CheckHitTypes.Contains(hitType.Enum),
                SelectedCommand = new ActionCommand(UpdateSmellyHitsView)
            });

            return filteredContentTypes.ToList();
        }

        private IEnumerable<FeatureType> CreateFilteredContentTypes()
        {
            // show all content types, but assign enabled and active based on the scanner configuration
            var filteredContentTypes = Config.ContentTypes.Select(contentType => new FeatureType
            {
                Description = contentType.Description,
                Tip = contentType.Tip,
                IsSupported = Model.Config.CheckContentTypes.Contains(contentType.Description),
                IsActive = Model.Config.CheckContentTypes.Contains(contentType.Description),
                SelectedCommand = new ActionCommand(UpdateSmellyHitsView)
            });

            return filteredContentTypes;
        }

        private void SearchTextChanged()
        {
            // delay processing text changed
            if (_searchTextChangedDelayTimer == null)
            {
                _searchTextChangedDelayTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(300)};
                _searchTextChangedDelayTimer.Tick += (_, _) =>
                {
                    _searchTextChangedDelayTimer.Stop();
                    SmellyGamesView.Refresh();
                };
            }

            _searchTextChangedDelayTimer.Stop(); // Resets the timer
            _searchTextChangedDelayTimer.Start();
        }

        private void UpdateSmellyStatus(IEnumerable<Game> games)
        {
            games.ForEach(game =>
            {
                // update smelly status of each game based AND filter the view based on the selected content and/or hit criteria
                game.Content.Update(
                    () => _filteredContentTypes.Where(x => x.IsActive).Select(x => x.Description),
                    () => _filteredHitTypes.Where(x => x.IsActive).Select(x => x.Description));
            });
        }

        private void UpdateSmellyHitsView()
        {
            Games.ForEach(game => game.Content.SmellyHitsView.Refresh());
            SmellyGamesView.Refresh();
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

            // text filter
            SmellyGamesView.Filter += gameObject =>
            {
                // only display games that have smelly hits AND those smelly hits haven't already been filtered out (e.g. filtered on content or hit type)
                if (((Game) gameObject).Content.SmellyHitsView.Count == 0)
                    return false;

                // return hits based on description match against the search text
                return SearchText.Length == 0 || ((Game) gameObject).Description.ToLower().Contains(SearchText.ToLower());

            };
        }

        private readonly IEnumerable<FeatureType> _filteredContentTypes;
        private readonly IEnumerable<FeatureType> _filteredHitTypes;

        private DispatcherTimer _searchTextChangedDelayTimer;
    }
}