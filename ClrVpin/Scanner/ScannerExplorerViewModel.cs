using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ClrVpin.Controls;
using ClrVpin.Models.Shared.Game;
using PropertyChanged;
using Utils;

namespace ClrVpin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class ScannerExplorerViewModel
    {
        public ScannerExplorerViewModel(ObservableCollection<GameDetail> gameDetails)
        {
            GameDetails = gameDetails;
            GameDetailsView = new ListCollectionView<GameDetail>(gameDetails);

            // text filter
            GameDetailsView.Filter += gameDetail => string.IsNullOrEmpty(SearchText) || gameDetail.Game.Description.ToLower().Contains(SearchText.ToLower());

            SearchTextCommand = new ActionCommand(SearchTextChanged);
        }

        public ListCollectionView<GameDetail> GameDetailsView { get; set; }

        public Window Window { get; private set; }
        public string SearchText { get; set; } = "";
        public ICommand SearchTextCommand { get; set; }
        public ObservableCollection<GameDetail> GameDetails { get; set; }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindowEx
            {
                Owner = parentWindow,
                Title = "Explorer (Tables)",
                Left = left,
                Top = top,
                Width = Model.ScreenWorkArea.Width - left - WindowMargin,
                Height = (Model.ScreenWorkArea.Height - WindowMargin - WindowMargin) / 3,
                MinWidth = 400,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ScannerExplorerTemplate") as DataTemplate
            };
            Window.Show();
        }


        public void Close() => Window.Close();

        private void SearchTextChanged()
        {
            // delay processing text changed
            if (_searchTextChangedDelayTimer == null)
            {
                _searchTextChangedDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
                _searchTextChangedDelayTimer.Tick += (_, _) =>
                {
                    _searchTextChangedDelayTimer.Stop();
                    GameDetailsView.Refresh();
                };
            }

            _searchTextChangedDelayTimer.Stop(); // Resets the timer
            _searchTextChangedDelayTimer.Start();
        }

        private DispatcherTimer _searchTextChangedDelayTimer;
        private const int WindowMargin = 0;
    }
}