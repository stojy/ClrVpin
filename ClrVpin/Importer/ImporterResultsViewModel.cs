using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ClrVpin.Controls;
using ClrVpin.Importer.Vps;
using MaterialDesignThemes.Wpf;
using PropertyChanged;
using Utils;

namespace ClrVpin.Importer
{
    [AddINotifyPropertyChangedInterface]
    public class ImporterResultsViewModel
    {
        public ImporterResultsViewModel(Game[] games)
        {
            // assign VM properties
            games.ForEach((game, index) =>
            {
                // index - for display
                game.Index = index;
                game.ImageUrlSelection = new UrlSelection()
                {
                    Url = game.ImgUrl,
                    SelectedCommand = new ActionCommand(() => ShowImage(game.ImgUrl))
                };

                game.TableFiles.Concat(game.B2SFiles).ForEach(imageFile =>
                {
                    imageFile.ImageUrlSelection = new UrlSelection()
                    {
                        Url = imageFile.ImgUrl,
                        SelectedCommand = new ActionCommand(() => ShowImage(imageFile.ImgUrl))
                    };
                });
            });

            Games = new ObservableCollection<Game>(games);
            GamesView = new ListCollectionView<Game>(Games);
        }
        
        private static void ShowImage(string tableImgUrl)
        {
            var imageUrlSelection = new UrlSelection
            {
                Url = tableImgUrl,
                SelectedCommand = new ActionCommand(() => DialogHost.Close("ImageDialog"))
            };

            DialogHost.Show(imageUrlSelection, "ImageDialog");
        }

        public ObservableCollection<Game> Games { get; set; }
        public ListCollectionView<Game> GamesView { get; set; }

        public Window Window { get; set; }

        public Game SelectedGame { get; set; }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindowEx
            {
                Owner = parentWindow,
                Title = "Results",
                Left = left,
                Top = top,
                Width = Model.ScreenWorkArea.Width - left - 5,
                Height = (Model.ScreenWorkArea.Height - 10) * 0.7,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ImporterResultsTemplate") as DataTemplate
            };
            Window.Show();
        }

        public void Close() => Window.Close();
    }
}