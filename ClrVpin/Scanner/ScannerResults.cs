﻿using System;
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

        private IEnumerable<FeatureType> CreateFilteredHitTypes()
        {
            // show all hit types, but assign enabled and active based on the scanner configuration
            var filteredContentTypes = Hit.Types.Select(hitType => new FeatureType
            {
                Description = hitType.GetDescription(),
                IsSupported = Config.CheckHitTypes.Contains(hitType),
                IsActive = Config.CheckHitTypes.Contains(hitType),
                SelectedCommand = new ActionCommand(UpdateSmellyHitsView)
            });

            return filteredContentTypes.ToList();
        }

        private IEnumerable<FeatureType> CreateFilteredContentTypes()
        {
            // show all content types, but assign enabled and active based on the scanner configuration
            var filteredContentTypes = Content.Types.Select(contentType => new FeatureType
            {
                Description = contentType,
                IsSupported = Config.CheckContentTypes.Contains(contentType),
                IsActive = Config.CheckContentTypes.Contains(contentType),
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
                if (SearchText.Length == 0)
                    return true;
                return ((Game) gameObject).Description.ToLower().Contains(SearchText.ToLower());
            };

            // don't display any games that don't have smelly hits - e.g. game smelly hits view filtered out by content and/or hit type
            SmellyGamesView.Filter += gameObject => ((Game) gameObject).Content.SmellyHitsView.Count > 0;
        }

        private readonly IEnumerable<FeatureType> _filteredContentTypes;
        private readonly IEnumerable<FeatureType> _filteredHitTypes;

        private readonly Window _parentWindow;
        private DispatcherTimer _searchTextChangedDelayTimer;
    }
}