using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using ClrVpin.Models;
using Utils;

namespace ClrVpin.Shared
{
    public abstract class Results
    {
        public ObservableCollection<Game> Games { get; set; }
        public ListCollectionView FilteredContentTypesView { get; set; }
        public ListCollectionView FilteredHitTypesView { get; set; }
        public ListCollectionView SmellyGamesView { get; set; }
        public ObservableCollection<Game> SmellyGames { get; set; }
        public ActionCommand<bool> ExpandGamesCommand { get; set; }
        public string SearchText { get; set; } = "";
        public ICommand SearchTextCommand { get; set; }
        public Window Window { get; set; }

        public void Close()
        {
            Window.Close();
        }

        protected void Initialise()
        {
            FilteredContentTypes = CreateFilteredContentTypes();
            FilteredContentTypesView = new ListCollectionView(FilteredContentTypes.ToList());

            FilteredHitTypes = CreateFilteredHitTypes();
            FilteredHitTypesView = new ListCollectionView(FilteredHitTypes.ToList());

            SearchTextCommand = new ActionCommand(SearchTextChanged);
            ExpandGamesCommand = new ActionCommand<bool>(ExpandItems);
            UpdateSmellyStatus(Games);
            InitSmellyGamesView();
        }

        protected abstract IEnumerable<FeatureType> CreateFilteredContentTypes();
        protected abstract IEnumerable<FeatureType> CreateFilteredHitTypes();

        protected void UpdateSmellyStatus(IEnumerable<Game> games)
        {
            games.ForEach(game =>
            {
                // update smelly status of each game based AND filter the view based on the selected content and/or hit criteria
                game.Content.Update(
                    () => FilteredContentTypes.Where(x => x.IsActive).Select(x => x.Description),
                    () => FilteredHitTypes.Where(x => x.IsActive).Select(x => x.Description));
            });
        }

        protected void UpdateSmellyHitsView()
        {
            Games.ForEach(game => game.Content.SmellyHitsView.Refresh());
            SmellyGamesView.Refresh();
        }

        protected void ExpandItems(bool expand)
        {
            SmellyGames.ForEach(game => game.IsExpanded = expand);
            SmellyGamesView.Refresh();
        }

        protected void InitSmellyGamesView()
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

        protected IEnumerable<FeatureType> FilteredContentTypes;
        protected IEnumerable<FeatureType> FilteredHitTypes;
        private DispatcherTimer _searchTextChangedDelayTimer;
    }
}