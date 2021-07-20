using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using ClrVpin.Models;
using ClrVpin.Models.Settings;
using Utils;

namespace ClrVpin.Shared
{
    public abstract class ResultsViewModel
    {
        // all games referenced in the DB.. irrespective of hits
        public ObservableCollection<Game> Games { get; set; }
        
        // games referenced in the DB that have hits
        public ObservableCollection<Game> HitGames { get; set; }

        public ListCollectionView FilteredContentTypesView { get; set; }
        public ListCollectionView FilteredHitTypesView { get; set; }
        public ListCollectionView HitGamesView { get; set; }

        public ICommand ExpandGamesCommand { get; set; }
        public string SearchText { get; set; } = "";
        public ICommand SearchTextCommand { get; set; }
        public Window Window { get; set; }

        public string BackupFolder { get; set; }
        public ICommand NavigateToBackupFolderCommand { get; set; }


        public void Close()
        {
            Window.Close();
        }

        protected void Initialise()
        {
            Settings = SettingsManager.Settings;

            FilteredContentTypes = CreateFilteredContentTypes();
            FilteredContentTypesView = new ListCollectionView(FilteredContentTypes.ToList());

            FilteredHitTypes = CreateFilteredHitTypes();
            FilteredHitTypesView = new ListCollectionView(FilteredHitTypes.ToList());

            SearchTextCommand = new ActionCommand(SearchTextChanged);
            ExpandGamesCommand = new ActionCommand<bool>(ExpandItems);

            BackupFolder = TableUtils.ActiveBackupFolder;
            NavigateToBackupFolderCommand = new ActionCommand(NavigateToBackupFolder);

            UpdateStatus(Games);
            InitView();
        }

        private void NavigateToBackupFolder() => Process.Start("explorer.exe", BackupFolder);

        protected abstract IList<FeatureType> CreateFilteredContentTypes();
        protected abstract IList<FeatureType> CreateFilteredHitTypes();
        protected Models.Settings.Settings Settings { get; set; }

        protected void UpdateStatus(IEnumerable<Game> games)
        {
            games.ForEach(game =>
            {
                // update status of each game based AND filter the view based on the selected content and/or hit criteria
                game.Content.Update(
                    () => FilteredContentTypes.Where(x => x.IsActive).Select(x => x.Id),
                    () => FilteredHitTypes.Where(x => x.IsActive).Select(x => x.Id));
            });
        }

        protected void UpdateHitsView()
        {
            Games.ForEach(game => game.Content.HitsView.Refresh());
            HitGamesView.Refresh();
        }

        protected void ExpandItems(bool expand)
        {
            HitGames.ForEach(game => game.IsExpanded = expand);
            HitGamesView.Refresh();
        }

        protected void InitView()
        {
            HitGames = new ObservableCollection<Game>(Games.Where(game => game.Content.Hits.Count > 0));
            HitGamesView = new ListCollectionView(HitGames);

            // text filter
            HitGamesView.Filter += gameObject =>
            {
                // only display games that have hits AND those hits haven't already been filtered out (e.g. filtered on content or hit type)
                if (((Game) gameObject).Content.HitsView.Count == 0)
                    return false;

                // return hits based on description match against the search text
                return string.IsNullOrEmpty(SearchText) || ((Game)gameObject).Description.ToLower().Contains(SearchText.ToLower());
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
                    HitGamesView.Refresh();
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