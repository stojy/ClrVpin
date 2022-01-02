using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
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
                game.ImageUrlSelection = new UrlSelection
                {
                    Url = game.ImgUrl,
                    SelectedCommand = new ActionCommand(() => ShowImage(game.ImgUrl))
                };

                // show large image popup
                var imageFiles = game.TableFiles.Concat(game.B2SFiles).ToList();
                imageFiles.ForEach(imageFile =>
                {
                    imageFile.ImageUrlSelection = new UrlSelection
                    {
                        Url = imageFile.ImgUrl,
                        SelectedCommand = new ActionCommand(() => ShowImage(imageFile.ImgUrl))
                    };
                });

                // navigate to url
                var allFiles = imageFiles
                    .Concat(game.RuleFiles)
                    .Concat(game.AltColorFiles)
                    .Concat(game.AltSoundFiles)
                    .Concat(game.MediaPackFiles)
                    .Concat(game.PovFiles)
                    .Concat(game.PupPackFiles)
                    .Concat(game.RomFiles)
                    .Concat(game.SoundFiles)
                    .Concat(game.TableFiles)
                    .Concat(game.TopperFiles)
                    .Concat(game.WheelArtFiles);
                allFiles.ForEach(file => { file.Urls.ForEach(url => url.SelectedCommand = new ActionCommand(() => NavigateToUrl(url.Url))); });
            });

            TablesFilterView = new ListCollectionView<string>(games.Select(x => x.Name).ToList());
            TablesFilterView.Filter = table =>
            {
                // filter the combo list
                var typedTable = (string)table;
                return TableFilter == null || typedTable.StartsWith(TableFilter, StringComparison.OrdinalIgnoreCase);
            };
            TablesFilterView.MoveCurrentTo(null);

            ManufacturersFilterView = new ListCollectionView<string>(games.Select(x => x.Manufacturer).ToList());
            YearsFilterView = new ListCollectionView<string>(games.Select(x => x.Year.ToString()).ToList());

            Games = new ObservableCollection<Game>(games);
            GamesView = new ListCollectionView<Game>(Games);
            GamesView.Filter = (game) =>
            {
                var typedGame = (Game)game;
                return TableFilter == null || typedGame.Name.StartsWith(TableFilter, StringComparison.OrdinalIgnoreCase);
            };

            FilterChanged = new ActionCommand(() =>
            {
                GamesView.Refresh();
                TablesFilterView.Refresh();
            });
        }

        // todo; move filters into a separate class
        public ListCollectionView<string> TablesFilterView { get; set; }
        public ListCollectionView<string> ManufacturersFilterView { get; set; }
        public ListCollectionView<string> YearsFilterView { get; set; }
        public string TableFilter { get; set; }

        public ObservableCollection<Game> Games { get; set; }
        public ListCollectionView<Game> GamesView { get; set; }

        public Window Window { get; private set; }

        public Game SelectedGame { get; set; }

        public ICommand FilterChanged { get; set; }

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

        private void NavigateToUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

        private static void ShowImage(string tableImgUrl)
        {
            var imageUrlSelection = new UrlSelection
            {
                Url = tableImgUrl,
                SelectedCommand = new ActionCommand(() => DialogHost.Close("ImageDialog"))
            };

            DialogHost.Show(imageUrlSelection, "ImageDialog");
        }
    }
}