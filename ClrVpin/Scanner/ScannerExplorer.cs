using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ClrVpin.Controls;
using ClrVpin.Models;
using MaterialDesignExtensions.Controls;
using PropertyChanged;
using Utils;

namespace ClrVpin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class ScannerExplorer
    {
        public ScannerExplorer(ObservableCollection<Game> games)
        {
            Games = games;
            GamesView = new ListCollectionView<Game>(games);
            
            // text filter
            GamesView.Filter += gameObject => string.IsNullOrEmpty(SearchText) || ((Game)gameObject).Description.ToLower().Contains(SearchText.ToLower());

            SearchTextCommand = new ActionCommand(SearchTextChanged);
        }

        public ListCollectionView<Game> GamesView { get; set; }

        public Window Window { get; private set; }
        public string SearchText { get; set; } = "";
        public ICommand SearchTextCommand { get; set; }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindow
            {
                Owner = parentWindow,
                Title = "Scanner Explorer",
                Left = left,
                Top = top,
                Width = Model.ScreenWorkArea.Width - left - 5,
                Height = (Model.ScreenWorkArea.Height - 10) / 3,
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
                _searchTextChangedDelayTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(300)};
                _searchTextChangedDelayTimer.Tick += (_, _) =>
                {
                    _searchTextChangedDelayTimer.Stop();
                    GamesView.Refresh();
                };
            }

            _searchTextChangedDelayTimer.Stop(); // Resets the timer
            _searchTextChangedDelayTimer.Start();
        }

        private DispatcherTimer _searchTextChangedDelayTimer;
        public ObservableCollection<Game> Games { get; set; }
    }
}