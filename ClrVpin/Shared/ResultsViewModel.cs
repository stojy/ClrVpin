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
using Utils;

namespace ClrVpin.Shared
{
    public abstract class ResultsViewModel
    {
        // all games referenced in the DB.. irrespective of hits
        public ObservableCollection<Game> Games { get; set; }
        
        // games referenced in the DB that have hits
        public ObservableCollection<Game> HitGames { get; set; }

        public ListCollectionView AllContentFeatureTypesView { get; set; }
        public ListCollectionView AllHitFeatureTypesView { get; set; }
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
            Settings = Model.Settings;

            AllContentFeatureTypes = CreateAllContentFeatureTypes();
            AllContentFeatureTypesView = new ListCollectionView(AllContentFeatureTypes.ToList());

            AllHitFeatureTypes = CreateAllHitFeatureTypes();
            AllHitFeatureTypesView = new ListCollectionView(AllHitFeatureTypes.ToList());

            SearchTextCommand = new ActionCommand(SearchTextChanged);
            ExpandGamesCommand = new ActionCommand<bool>(ExpandItems);

            BackupFolder = FileUtils.ActiveBackupFolder;
            NavigateToBackupFolderCommand = new ActionCommand(NavigateToBackupFolder);

            UpdateStatus(Games);
            InitView();
        }

        private void NavigateToBackupFolder() => Process.Start("explorer.exe", BackupFolder);

        protected abstract IList<FeatureType> CreateAllContentFeatureTypes();
        protected abstract IList<FeatureType> CreateAllHitFeatureTypes();
        protected Models.Settings.Settings Settings { get; set; }

        protected void UpdateStatus(IEnumerable<Game> games)
        {
            games.ForEach(game =>
            {
                // update status of each game based AND filter the view based on the selected content and/or hit criteria
                game.Content.Update(
                    () => AllContentFeatureTypes.Where(x => x.IsActive).Select(x => x.Id),
                    () => AllHitFeatureTypes.Where(x => x.IsActive).Select(x => x.Id));
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

        protected IEnumerable<FeatureType> AllContentFeatureTypes;
        protected IEnumerable<FeatureType> AllHitFeatureTypes;
        private DispatcherTimer _searchTextChangedDelayTimer;
    }
}