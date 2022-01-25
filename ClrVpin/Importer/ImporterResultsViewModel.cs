using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
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
                // patch the data.. assign a top level image url if one doesn't already exist
                game.ImgUrl ??= game.B2SFiles.FirstOrDefault(x => x.ImgUrl != null)?.ImgUrl ?? game.TableFiles.FirstOrDefault(x => x.ImgUrl != null)?.ImgUrl;

                // enforce sorted file ordering
                // - required for some tables, e.g. 300
                game.TableFiles = game.TableFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.B2SFiles = game.B2SFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.WheelArtFiles = game.WheelArtFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.RomFiles = game.RomFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.MediaPackFiles = game.MediaPackFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.AltColorFiles = game.AltColorFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.SoundFiles = game.SoundFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.TopperFiles = game.TopperFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.PupPackFiles = game.PupPackFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.PovFiles = game.PovFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.AltSoundFiles = game.AltSoundFiles.OrderByDescending(x => x.UpdatedAt).ToArray();
                game.RuleFiles = game.RuleFiles.OrderByDescending(x => x.UpdatedAt).ToArray();

                // index - for display
                game.Index = index + 1;
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

                game.YearString = game.Year.ToString();

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

            // main games view (data grid)
            Games = new ObservableCollection<Game>(games);
            GamesView = new ListCollectionView<Game>(Games)
            {
                // filter the table names list to reflect the various criteria options
                Filter = game => 
                    (TableFilter == null || game.Name.Contains(TableFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (ManufacturerFilter == null || game.Manufacturer.Contains(ManufacturerFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (IncludeOriginals || !game.Manufacturer.StartsWith("Original", StringComparison.InvariantCultureIgnoreCase)) &&
                    (YearFilter == null || game.YearString.StartsWith(YearFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (UpdatedDateBegin == null || game.UpdatedAt == null || game.UpdatedAt.Value >= UpdatedDateBegin) &&
                    (UpdatedDateEnd == null || game.UpdatedAt == null || game.UpdatedAt.Value < UpdatedDateEnd.Value.AddDays(1))
            };

            // filters views (drop down combo boxes)
            TablesFilterView = new ListCollectionView<string>(games.Select(x => x.Name).OrderBy(x => x).ToList())
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = table => GamesView.Any(x => x.Name == table)
            };

            ManufacturersFilterView = new ListCollectionView<string>(games.Select(x => x.Manufacturer).Distinct().OrderBy(x => x).ToList())
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = manufacturer => GamesView.Any(x => x.Manufacturer == manufacturer)
            };

            YearsView = new ListCollectionView<string>(games.Select(x => x.YearString).Distinct().OrderBy(x => x).ToList())
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = yearString => GamesView.Any(x => x.YearString.StartsWith(yearString))
            };

            // generic handler for all the filter changes.. since all of the combo box values will need to be re-evaluated in sync anyway
            FilterChanged = new ActionCommand(() =>
            {
                GamesView.Refresh();

                TablesFilterView.Refresh();
                ManufacturersFilterView.Refresh();
                YearsView.Refresh();
            });
        }

        // todo; move filters into a separate class
        public ListCollectionView<string> TablesFilterView { get; set; }
        public ListCollectionView<string> ManufacturersFilterView { get; set; }
        public ListCollectionView<string> YearsView { get; set; }
        
        public string TableFilter { get; set; }
        public string ManufacturerFilter { get; set; }
        public string YearFilter { get; set; }

        public ObservableCollection<Game> Games { get; set; }
        public ListCollectionView<Game> GamesView { get; set; }

        public Window Window { get; private set; }

        public Game SelectedGame { get; set; }

        public ICommand FilterChanged { get; set; }

        public bool IncludeOriginals { get; set; }

        public DateTime? UpdatedDateBegin { get; set; }
        public DateTime? UpdatedDateEnd { get; set; }

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