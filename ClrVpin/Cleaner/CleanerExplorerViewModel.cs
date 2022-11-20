using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Models.Shared.Game;
using PropertyChanged;
using Utils;

namespace ClrVpin.Cleaner
{
    [AddINotifyPropertyChangedInterface]
    public class CleanerExplorerViewModel
    {
        public CleanerExplorerViewModel(ObservableCollection<LocalGame> localGames)
        {
            LocalGames = localGames;
            LocalGamesView = new ListCollectionView<LocalGame>(localGames);

            // text filter
            LocalGamesView.Filter += localGame => string.IsNullOrEmpty(SearchText) || localGame.Game.Description.ToLower().Contains(SearchText.ToLower());

            SearchTextCommand = new ActionCommand(SearchTextChanged);
        }

        public ListCollectionView<LocalGame> LocalGamesView { get; }

        public Window Window { get; private set; }
        public string SearchText { get; set; } = "";
        public ICommand SearchTextCommand { get; set; }
        public ObservableCollection<LocalGame> LocalGames { get; }

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
                ContentTemplate = parentWindow.FindResource("CleanerExplorerTemplate") as DataTemplate
            };
            Window.Show();
        }


        public void Close() => Window.Close();

        private void SearchTextChanged() => LocalGamesView.RefreshDebounce();

        private const int WindowMargin = 0;
    }
}