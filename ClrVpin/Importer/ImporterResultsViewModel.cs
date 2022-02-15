using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Importer.Vps;
using ClrVpin.Models.Settings;
using MaterialDesignThemes.Wpf;
using PropertyChanged;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    [AddINotifyPropertyChangedInterface]
    public class ImporterResultsViewModel
    {
        public ImporterResultsViewModel(List<Game> games)
        {
            // assign VM properties
            games.ForEach(game =>
            {
                // index - for display
                game.ImageUrlSelection = new UrlSelection
                {
                    Url = game.ImgUrl,
                    SelectedCommand = new ActionCommand(() => ShowImage(game.ImgUrl))
                };

                // show large image popup
                game.ImageFiles.ForEach(imageFile =>
                {
                    imageFile.ImageUrlSelection = new UrlSelection
                    {
                        Url = imageFile.ImgUrl,
                        SelectedCommand = new ActionCommand(() => ShowImage(imageFile.ImgUrl))
                    };
                });

                game.YearString = game.Year.ToString();

                // navigate to url
                game.AllFiles.Select(x => x.Value).SelectMany(x => x).ForEach(file => { file.Urls.ForEach(url => url.SelectedCommand = new ActionCommand(() => NavigateToUrl(url.Url))); });
            });

            // main games view (data grid)
            Games = new ObservableCollection<Game>(games);
            GamesView = new ListCollectionView<Game>(Games)
            {
                // filter the table names list to reflect the various criteria options
                Filter = game =>
                    (TableFilter == null || game.Name.Contains(TableFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (ManufacturerFilter == null || game.Manufacturer.Contains(ManufacturerFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (Settings.IncludeOriginalTables || !game.Manufacturer.StartsWith("Original", StringComparison.InvariantCultureIgnoreCase)) &&
                    (YearBeginFilter == null || string.Compare(game.YearString, YearBeginFilter, StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (YearEndFilter == null || string.Compare(game.YearString, YearEndFilter, StringComparison.OrdinalIgnoreCase) <= 0) &&
                    (TypeFilter == null || game.Type?.Equals(TypeFilter, StringComparison.OrdinalIgnoreCase) == true) &&
                    (Settings.UpdatedAtDateBegin == null || game.UpdatedAt == null || game.UpdatedAt.Value >= Settings.UpdatedAtDateBegin) &&
                    (Settings.UpdatedAtDateEnd == null || game.UpdatedAt == null || game.UpdatedAt.Value < Settings.UpdatedAtDateEnd.Value.AddDays(1))
            };
            GamesView.MoveCurrentToFirst();

            // filters views (drop down combo boxes)
            TablesFilterView = new ListCollectionView<string>(games.Select(x => x.Name).Distinct().OrderBy(x => x).ToList())
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = table => GamesView.Any(x => x.Name == table)
            };

            ManufacturersFilterView = new ListCollectionView<string>(games.Select(x => x.Manufacturer).Distinct().OrderBy(x => x).ToList())
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = manufacturer => GamesView.Any(x => x.Manufacturer == manufacturer)
            };

            YearsBeginFilterView = new ListCollectionView<string>(games.Select(x => x.YearString).Distinct().Where(x => x != null).OrderBy(x => x).ToList())
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = yearString => GamesView.Any(x => x.YearString == yearString)
            };

            YearsEndFilterView = new ListCollectionView<string>(games.Select(x => x.YearString).Distinct().Where(x => x != null).OrderBy(x => x).ToList())
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = yearString => GamesView.Any(x => x.YearString == yearString)
            };

            TypesFilterView = new ListCollectionView<string>(games.Select(x => x.Type).Distinct().Where(x => x != null).OrderBy(x => x).ToList());

            // generic handler for all the filter changes.. since all of the combo box values will need to be re-evaluated in sync anyway
            FilterChanged = new ActionCommand(() =>
            {
                // update main list
                GamesView.Refresh();

                // update filters based on what is shown in the main list
                TablesFilterView.Refresh();
                ManufacturersFilterView.Refresh();
                YearsBeginFilterView.Refresh();
                YearsEndFilterView.Refresh();
                TypesFilterView.Refresh();
            });

            UpdatedFilterChanged = new ActionCommand(() =>
            {
                UpdateIsNew();
                FilterChanged.Execute(null);
            });

            NavigateToIpdbCommand = new ActionCommand<string>(url => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }));

            UpdateIsNew();
        }

        private void UpdateIsNew()
        {
            // flag models if they satisfy the update time range
            Games.ForEach(game => game.AllFiles.ForEach(kv =>
            {
                var (_, files) = kv;
                files.ForEach(file =>
                {
                    // flag file - if the update time range is satisfied
                    file.IsNew = file.UpdatedAt >= (Settings.UpdatedAtDateBegin ?? DateTime.MinValue) && file.UpdatedAt <= (Settings.UpdatedAtDateEnd?.AddDays(1) ?? DateTime.Now);

                    // flag each url within the file - required to allow for simpler view binding
                    file.Urls.ForEach(url => url.IsNew = file.IsNew);
                });

                // flag file collection (e.g. backglasses)
                files.IsNew = files.Any(file => file.IsNew);
            }));
        }

        public ImporterSettings Settings { get; } = Model.Settings.Importer;

        // todo; move filters into a separate class
        public ListCollectionView<string> TablesFilterView { get; set; }
        public ListCollectionView<string> ManufacturersFilterView { get; set; }
        public ListCollectionView<string> YearsBeginFilterView { get; set; }
        public ListCollectionView<string> YearsEndFilterView { get; set; }
        public ListCollectionView<string> TypesFilterView { get; set; }

        public string TableFilter { get; set; }
        public string ManufacturerFilter { get; set; }
        public string YearBeginFilter { get; set; }
        public string YearEndFilter { get; set; }
        public string TypeFilter { get; set; }

        public ObservableCollection<Game> Games { get; set; }
        public ListCollectionView<Game> GamesView { get; set; }

        public Window Window { get; private set; }

        public Game SelectedGame { get; set; }

        public ICommand FilterChanged { get; set; }
        public ICommand UpdatedFilterChanged { get; set; }

        public ICommand NavigateToIpdbCommand { get; set; }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindowEx
            {
                Owner = parentWindow,
                Title = "Results",
                Left = left,
                Top = top,
                Width = Model.ScreenWorkArea.Width - left - WindowMargin,
                Height = (Model.ScreenWorkArea.Height - 10) * 0.73,
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

        private const int WindowMargin = 0;
    }
}