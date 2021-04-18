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
            _parentWindow = parentWindow;

            Games = games;

            SearchTextCommand = new ActionCommand(SearchTextChanged);
            ExpandGamesCommand = new ActionCommand<bool>(ExpandItems);

            //FilterContentTypeCommand = new ActionCommand(FilterContentType);
            FilterHitTypeCommand = new ActionCommand<HitType>(FilterHitType);

            _filteredContentTypes = CreateFilteredContentTypes();
            FilteredContentTypesView = new ListCollectionView(_filteredContentTypes);

            UpdateSmellyStatus(Games);
            InitSmellyGamesView();
        }

        public ListCollectionView FilteredContentTypesView { get; set; }

        public ObservableCollection<Game> Games { get; set; }
        public ListCollectionView SmellyGamesView { get; set; }
        public ObservableCollection<Game> SmellyGames { get; set; }
        public ActionCommand<bool> ExpandGamesCommand { get; set; }
        public ActionCommand<HitType> FilterHitTypeCommand { get; set; }
        public string SearchText { get; set; } = "";
        public ICommand SearchTextCommand { get; set; }

        public Window Window { get; set; }

        public void Show()
        {
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

        private List<FeatureType> CreateFilteredContentTypes()
        {
            // show all content types, but enabled and active based on the scanner configuration
            var filteredContentTypes = Content.Types.Select(contentType => new FeatureType
            {
                Description = contentType,
                IsSupported = Config.CheckContentTypes.Contains(contentType),
                IsActive = Config.CheckContentTypes.Contains(contentType),
                SelectedCommand = new ActionCommand(() => FilterContentType())
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

        private void UpdateSmellyStatus(IEnumerable<Game> games)
        {
            games.ForEach(game =>
            {
                // update smelly status
                game.Content.Update(() => _filteredContentTypes.Where(x => x.IsActive).Select(x => x.Description), () => _filteredHitTypes);
            });
        }

        private void FilterContentType()
        {
            Games.ForEach(game => game.Content.SmellyHitsView.Refresh());
            SmellyGamesView.Refresh();
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

            // text filter
            SmellyGamesView.Filter += gameObject =>
            {
                if (SearchText.Length == 0)
                    return true;
                return ((Game) gameObject).Description.ToLower().Contains(SearchText.ToLower());
            };

            // don't display any games that don't have smelly hits - e.g. game smelly hits view filtered out by content and/or hit type
            SmellyGamesView.Filter += gameObject => ((Game) gameObject).Content.SmellyHitsView.Count > 0;
        }

        private readonly List<FeatureType> _filteredContentTypes;

        private readonly List<HitType> _filteredHitTypes = new List<HitType>(Hit.Types);

        private readonly Window _parentWindow;
        private DispatcherTimer _searchTextChangedDelayTimer;
    }
}