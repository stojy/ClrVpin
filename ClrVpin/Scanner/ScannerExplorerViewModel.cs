using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
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

        public ListCollectionView<GameDetail> GameDetailsView { get; }

        public Window Window { get; private set; }
        public string SearchText { get; set; } = "";
        public ICommand SearchTextCommand { get; set; }
        public ObservableCollection<GameDetail> GameDetails { get; }

        public void Show(Window parentWindow, double left, double top, double width)
        {
            Window = new MaterialWindowEx
            {
                Owner = parentWindow,
                Title = "Explorer (Tables)",
                Left = left,
                Top = top,
                Width = width,
                Height = (Model.ScreenWorkArea.Height - WindowMargin - WindowMargin) / 3,
                MinWidth = 400,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ScannerExplorerTemplate") as DataTemplate
            };
            Window.Show();
        }


        public void Close() => Window.Close();

        private void SearchTextChanged() => GameDetailsView.RefreshDebounce();

        private const int WindowMargin = 0;
    }
}