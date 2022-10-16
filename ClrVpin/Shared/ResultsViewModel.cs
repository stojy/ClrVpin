using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Shared
{
    public abstract class ResultsViewModel
    {
        // all games referenced in the DB.. irrespective of hits
        public ObservableCollection<LocalGame> Games { get; protected init; }

        public ListCollectionView<FeatureType> AllContentFeatureTypesView { get; private set; }
        public ListCollectionView<FeatureType> AllHitFeatureTypesView { get; private set; }
        public ListCollectionView<LocalGame> HitGamesView { get; private set; }

        public ICommand ExpandGamesCommand { get; private set; }
        public string SearchText { get; set; } = "";
        public ICommand SearchTextCommand { get; set; }
        public Window Window { get; protected set; }

        public string BackupFolder { get; private set; }
        public ICommand NavigateToBackupFolderCommand { get; private set; }
        protected Models.Settings.Settings Settings { get; private set; }

        public void Close()
        {
            Window.Close();
        }

        protected void Initialise()
        {
            Settings = Model.Settings;

            _allContentFeatureTypes = CreateAllContentFeatureTypes();
            AllContentFeatureTypesView = new ListCollectionView<FeatureType>(_allContentFeatureTypes.ToList());

            _allHitFeatureTypes = CreateAllHitFeatureTypes();
            AllHitFeatureTypesView = new ListCollectionView<FeatureType>(_allHitFeatureTypes.ToList());

            SearchTextCommand = new ActionCommand(SearchTextChanged);
            ExpandGamesCommand = new ActionCommand<bool>(ExpandItems);

            BackupFolder = FileUtils.ActiveBackupFolder;
            NavigateToBackupFolderCommand = new ActionCommand(NavigateToBackupFolder);

            UpdateStatus(Games);
            InitView();
        }

        protected abstract IList<FeatureType> CreateAllContentFeatureTypes();
        protected abstract IList<FeatureType> CreateAllHitFeatureTypes();

        protected void UpdateHitsView()
        {
            Games.ForEach(game => game.Content.HitsView.Refresh());
            HitGamesView.Refresh();
        }

        protected static string CreatePercentageStatistic(string title, int count, int totalCount)
        {
            var percentage = totalCount == 0 ? 0 : 100f * count / totalCount;
            return $"{title}:  {count} of {totalCount} ({percentage:F2}%)";
        }

        private void NavigateToBackupFolder() => Process.Start("explorer.exe", BackupFolder);

        private void UpdateStatus(IEnumerable<LocalGame> games)
        {
            games.ForEach(game =>
            {
                // update status of each game based AND filter the view based on the selected content and/or hit criteria
                game.Content.Update(
                    () => _allContentFeatureTypes.Where(x => x.IsActive).Select(x => x.Id),
                    () => _allHitFeatureTypes.Where(x => x.IsActive).Select(x => x.Id));
            });
        }

        private void ExpandItems(bool expand)
        {
            _hitGames.ForEach(game => game.ViewState.IsExpanded = expand);
            HitGamesView.Refresh();
        }

        private void InitView()
        {
            _hitGames = new ObservableCollection<LocalGame>(Games.Where(game => game.Content.Hits.Count > 0));
            HitGamesView = new ListCollectionView<LocalGame>(_hitGames);

            // text filter
            HitGamesView.Filter += localGame =>
            {
                // only display games that have hits AND those hits haven't already been filtered out (e.g. filtered on content or hit type)
                if (localGame.Content.HitsView.Count == 0)
                    return false;

                // return hits based on description match against the search text
                return string.IsNullOrEmpty(SearchText) || localGame.Game.Description.ToLower().Contains(SearchText.ToLower());
            };
        }

        private void SearchTextChanged() => HitGamesView.RefreshDebounce();

        // games referenced in the DB that have hits
        private ObservableCollection<LocalGame> _hitGames;

        private IEnumerable<FeatureType> _allContentFeatureTypes;
        private IEnumerable<FeatureType> _allHitFeatureTypes;

        protected const string DialogHostName = "ResultsDialog";
    }
}