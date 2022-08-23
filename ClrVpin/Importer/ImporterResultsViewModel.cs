using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Logging;
using ClrVpin.Models.Importer;
using ClrVpin.Models.Importer.Vps;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using MaterialDesignThemes.Wpf;
using PropertyChanged;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Importer
{
    public interface IOnlineGameCollections
    {
        List<string> Manufacturers { get; }
        List<string> Types { get; }
        List<string> Years { get; }
        List<int?> Players { get; }
        List<string> Roms { get; }
        List<string> Themes { get; }
        List<string> Authors { get; }
    }

    [AddINotifyPropertyChangedInterface]
    public class ImporterResultsViewModel : IOnlineGameCollections
    {
        public ImporterResultsViewModel(IList<GameItem> gameItems, IList<GameDetail> localGames, ImporterMatchStatistics matchStatistics)
        {
            // use the supplied localGames list instead of extracting from gameItems to ensure the existing ordering in the DB file(s) is preserved
            // - we don't want to re-order based on the online feed (after the various importer fixes) as this makes it too difficult to track the differences
            // - _localGames = gameItems.Where(item => item.GameDetail != null).Select(item => item.GameDetail).ToList();
            _localGames = localGames;

            _matchStatistics = matchStatistics.ToDictionary();
            IsMatchingEnabled = Model.Settings.Importer.SelectedMatchCriteriaOptions.Any();

            TableStyleOptionsView = new ListCollectionView<FeatureType>(CreateTableStyleOptions().ToList());
            TableMatchOptionsView = new ListCollectionView<FeatureType>(CreateTableMatchOptions().ToList());

            // assign VM properties
            gameItems.ForEach(gameItem =>
            {
                if (gameItem.OnlineGame is not { } onlineGame) // pattern matching - assign AND check for not null!
                    return;

                // image - for showing dialog with larger view of image
                onlineGame.ImageUrlSelection = new UrlSelection
                {
                    Url = onlineGame.ImgUrl,
                    SelectedCommand = new ActionCommand(() => ShowImage(onlineGame.ImgUrl))
                };

                // local database show/add commands
                onlineGame.UpdateDatabaseEntryCommand = new ActionCommand(() =>
                    DatabaseItemManagement.UpdateDatabaseItem(_localGames, onlineGame, this));
                onlineGame.CreateDatabaseEntryCommand = new ActionCommand(() =>
                    DatabaseItemManagement.CreateDatabaseItem(_localGames, onlineGame, this));

                // show large image popup
                onlineGame.ImageFiles.ForEach(imageFile =>
                {
                    imageFile.ImageUrlSelection = new UrlSelection
                    {
                        Url = imageFile.ImgUrl,
                        SelectedCommand = new ActionCommand(() => ShowImage(imageFile.ImgUrl))
                    };
                });

                onlineGame.IsMatchingEnabled = IsMatchingEnabled;
                onlineGame.UpdateDatabaseEntryTooltip += IsMatchingEnabled ? "" : MatchingDisabledMessage;
                onlineGame.CreateDatabaseEntryTooltip += IsMatchingEnabled ? "" : MatchingDisabledMessage;

                // extract IpdbId
                var match = _regexExtractIpdbId.Match(onlineGame.IpdbUrl ?? string.Empty);
                if (match.Success)
                    onlineGame.IpdbId = match.Groups["ipdbId"].Value;

                onlineGame.IsMatched = onlineGame.Hit != null;

                // navigate to url
                onlineGame.AllFiles.Select(x => x.Value).SelectMany(x => x).ForEach(file => { file.Urls.ForEach(url => url.SelectedCommand = new ActionCommand(() => NavigateToUrl(url.Url))); });
            });

            // main games view (data grid)
            GameItems = new ObservableCollection<GameItem>(gameItems);
            GameItemsView = new ListCollectionView<GameItem>(GameItems)
            {
                // filter the table names list to reflect the various view filtering criteria
                Filter = game =>
                    (TableFilter == null || game.Name.Contains(TableFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (ManufacturerFilter == null || game.Manufacturer.Contains(ManufacturerFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (Settings.SelectedTableStyleOption == TableStyleOptionEnum.Both ||
                     (Settings.SelectedTableStyleOption == TableStyleOptionEnum.Manufactured && !game.IsOriginal) ||
                     (Settings.SelectedTableStyleOption == TableStyleOptionEnum.Original && game.IsOriginal)) &&
                    (Settings.SelectedTableMatchOption == TableMatchOptionEnum.All ||
                     (Settings.SelectedTableMatchOption == TableMatchOptionEnum.LocalAndOnline && game.OnlineGame != null && game.GameDetail != null) ||
                     (Settings.SelectedTableMatchOption == TableMatchOptionEnum.LocalOnly && game.OnlineGame == null) ||
                     (Settings.SelectedTableMatchOption == TableMatchOptionEnum.OnlineOnly && game.GameDetail == null)) &&
                    (YearBeginFilter == null || string.Compare(game.OnlineGame?.YearString, YearBeginFilter, StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (YearEndFilter == null || string.Compare(game.OnlineGame?.YearString, YearEndFilter, StringComparison.OrdinalIgnoreCase) <= 0) &&
                    (TypeFilter == null || game.Type?.Equals(TypeFilter, StringComparison.OrdinalIgnoreCase) == true) &&
                    (Settings.UpdatedAtDateBegin == null || game.UpdatedAt == null || game.UpdatedAt.Value >= Settings.UpdatedAtDateBegin) &&
                    (Settings.UpdatedAtDateEnd == null || game.UpdatedAt == null || game.UpdatedAt.Value < Settings.UpdatedAtDateEnd.Value.AddDays(1))
            };
            GameItemsView.MoveCurrentToFirst();

            // filters views (drop down combo boxes) - uses the online AND unmatched local DB 
            TablesFilterView = new ListCollectionView<string>(gameItems.Select(x => x.Name).Distinct().OrderBy(x => x).ToList())
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = table => GameItemsView.Any(x => x.Name == table)
            };

            Manufacturers = gameItems.Select(x => x.Manufacturer).Distinct().OrderBy(x => x).ToList();
            ManufacturersFilterView = new ListCollectionView<string>(Manufacturers)
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = manufacturer => GameItemsView.Any(x => x.Manufacturer == manufacturer)
            };

            Years = gameItems.Select(x => x.Year).Distinct().Where(x => x != null).OrderBy(x => x).ToList();
            YearsBeginFilterView = new ListCollectionView<string>(Years)
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = yearString => GameItemsView.Any(x => x.Year == yearString)
            };

            YearsEndFilterView = new ListCollectionView<string>(Years)
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = yearString => GameItemsView.Any(x => x.Year == yearString)
            };

            Types = gameItems.Select(x => x.Type).Distinct().Where(x => x != null).OrderBy(x => x).ToList();
            TypesFilterView = new ListCollectionView<string>(Types);

            Players = gameItems.Select(x => x.OnlineGame?.Players).Distinct().Where(x => x != null).OrderBy(x => x).ToList();
            Roms = gameItems.Select(x => x.OnlineGame?.RomFiles?.FirstOrDefault()?.Name).Distinct().Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x).ToList();
            Themes = gameItems.Select(x => string.Join(", ", x.OnlineGame?.Themes ?? new[] { "" })).Distinct().Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x).ToList();

            var tablesWithAuthors = gameItems.Select(x => x.OnlineGame?.TableFiles.Select(table => string.Join(", ", table.Authors.OrderBy(author => author)))).Where(x => x != null).SelectMany(x => x);
            Authors = tablesWithAuthors.Distinct().Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x).ToList();

            // generic handler for all the filter changes.. since all of the combo box values will need to be re-evaluated in sync anyway
            FilterChanged = new ActionCommand(() =>
            {
                // update main list
                GameItemsView.Refresh();

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

            BackupFolder = Model.Settings.BackupFolder;
            NavigateToBackupFolderCommand = new ActionCommand(NavigateToBackupFolder);

            AddMissingDatabaseInfoTip += "Add any missing information in your local database from online sources" + (IsMatchingEnabled ? "" : MatchingDisabledMessage);
            AllTableAddMissingDatabaseInfoCommand = new ActionCommand(AllTableAddMissingDatabaseProperties);

            OverwriteDatabaseInfoTip += "Overwrite all information in your local database from online sources. Information that doesn't exist from online sources will not be overwritten (e.g. ratings)." +
                                        (IsMatchingEnabled ? "" : MatchingDisabledMessage);
            AllTableOverwriteDatabaseInfoCommand = new ActionCommand(AllTableOverwriteDatabaseProperties);

            // assign a convenience property to avoid a *lot* of nested referenced in the xaml
            GameItemSelectedCommand = new ActionCommand(() => SelectedOnlineGame = SelectedGameItem?.OnlineGame);
        }


        public string AddMissingDatabaseInfoTip { get; }
        public string OverwriteDatabaseInfoTip { get; }

        public ListCollectionView<FeatureType> TableStyleOptionsView { get; }
        public ListCollectionView<FeatureType> TableMatchOptionsView { get; }

        public string BackupFolder { get; }
        public ICommand NavigateToBackupFolderCommand { get; }

        public ImporterSettings Settings { get; } = Model.Settings.Importer;

        // todo; move filters into a separate class?
        public ListCollectionView<string> TablesFilterView { get; }
        public ListCollectionView<string> ManufacturersFilterView { get; }
        public ListCollectionView<string> YearsBeginFilterView { get; }
        public ListCollectionView<string> YearsEndFilterView { get; }
        public ListCollectionView<string> TypesFilterView { get; }

        public string TableFilter { get; set; }
        public string ManufacturerFilter { get; set; }
        public string YearBeginFilter { get; set; }
        public string YearEndFilter { get; set; }
        public string TypeFilter { get; set; }

        public ObservableCollection<GameItem> GameItems { get; }
        public ListCollectionView<GameItem> GameItemsView { get; }

        public Window Window { get; private set; }

        public GameItem SelectedGameItem { get; set; }
        public OnlineGame SelectedOnlineGame { get; set; }

        public ICommand FilterChanged { get; set; }
        public ICommand UpdatedFilterChanged { get; set; }

        public ICommand NavigateToIpdbCommand { get; }
        public ICommand AllTableAddMissingDatabaseInfoCommand { get; }
        public ICommand AllTableOverwriteDatabaseInfoCommand { get; }
        public ICommand GameItemSelectedCommand { get; }

        public bool IsMatchingEnabled { get; }


        // IOnlineGameCollections
        public List<string> Manufacturers { get; }
        public List<string> Types { get; }
        public List<string> Years { get; }
        public List<int?> Players { get; }
        public List<string> Roms { get; }
        public List<string> Themes { get; }
        public List<string> Authors { get; }

        public async Task Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindowEx
            {
                Owner = parentWindow,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Title = "Results",
                Left = left,
                Top = top,
                Width = Model.ScreenWorkArea.Width - WindowMargin,
                Height = (Model.ScreenWorkArea.Height - 10) * 0.73,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ImporterResultsTemplate") as DataTemplate
            };

            Window.Show();

            await ShowSummary();
        }

        public void Close()
        {
            Model.SettingsManager.Write();
            Window.Close();
        }

        private async Task ShowSummary()
        {
            if (!IsMatchingEnabled)
            {
                await Notification.ShowWarning(DialogHostName, "Reduced Functionality", "Because fuzzy matching was not enabled.");
                return;
            }

            // simplified summary of the ImporterStatisticsViewModel info
            var onlineManufacturedCount = GameItems.Count(game => !game.OnlineGame?.IsOriginal == true);

            var detail = CreatePercentageStatistic("Missing Manufactured Tables", _matchStatistics[ImporterMatchStatistics.UnmatchedOnlineManufactured], onlineManufacturedCount);

            var isSuccess = onlineManufacturedCount == _matchStatistics[ImporterMatchStatistics.MatchedManufactured];
            await (isSuccess ? Notification.ShowSuccess(DialogHostName, "All Manufactured Tables Present") : Notification.ShowWarning(DialogHostName, "Manufactured Tables Missing", null, detail));
        }

        private static string CreatePercentageStatistic(string title, int count, int totalCount) => $"{title} : {count} of {totalCount} ({100f * count / totalCount:F2}%)";

        private async void AllTableAddMissingDatabaseProperties()
        {
            await AllTableUpdateDatabase(false);
        }

        private async void AllTableOverwriteDatabaseProperties()
        {
            await AllTableUpdateDatabase(true);
        }

        private async Task AllTableUpdateDatabase(bool overwriteProperties)
        {
            if (overwriteProperties)
            {
                var result = await Notification.ShowConfirmation(DialogHostName,
                    "Overwrite All Info In Your Database Files From Online Sources",
                    "This will fix incorrect and out of date information in your local database.\n\n" +
                    "Please read carefully before proceeding.",
                    "1. Before starting, run Scanner to confirm your collection is clean.\n" +
                    "2. During the process, all the local database info is updated from online sources¹²³.\n" +
                    "3. After completing, run Scanner to clean your collection (e.g. rename files).\n" +
                    "\n" +
                    "¹ The database file(s) are automatically backed up before any changes are made.\n" +
                    "² Information that doesn't exist from online sources will not be overwritten (e.g. ratings)\n" +
                    "³ In extreme cases, if your local database had substantially different values for 'name' and 'description',\n" +
                    "  then Scanner may not be able to automatically rename the files.  You can fix this by either..\n" +
                    "  a. Run Scanner with trainer wheels to identify the files, then manually rename the files.\n" +
                    "  b. Run Scanner without trainer wheels, then rename files (in the backup folder), then run Importer\n" +
                    "     to merge the files back into your collection."
                    , true, "Update Now", "Maybe Later");

                if (result != true)
                    return;
            }

            var (propertyStatistics, updatedGameCount, matchedGameCount) = GameUpdater.UpdateProperties(GetOnlineGames(), overwriteProperties);

            // write ALL local game entries back to the database
            // - updated properties via OnlineGames.Hit.GameDetail are reflected in the local game entries
            // - write irrespective of whether matched or not so that no entries are lost
            if (updatedGameCount > 0)
                TableUtils.WriteGamesToDatabase(_localGames.Select(x => x.Game));

            Logger.Info($"Added missing database info: table count: {updatedGameCount}, info count: {GameUpdater.GetPropertiesUpdatedCount(propertyStatistics)}");

            var properties = propertyStatistics.Select(property => $"- {property.Key,-13}: {property.Value}").StringJoin("\n");
            var details = CreatePercentageStatistic("Tables Fixed  ", updatedGameCount, matchedGameCount) +
                          $"\n{properties}";

            var isSuccess = updatedGameCount == 0;
            if (isSuccess)
                await Notification.ShowSuccess(DialogHostName, "No Updates Required");
            else
                await Notification.ShowSuccess(DialogHostName, "Tables Updated", null, details);
        }

        private IEnumerable<FeatureType> CreateTableStyleOptions()
        {
            // all table style options
            var featureTypes = StaticSettings.TableStyleOptions.Select(tableStyleOption =>
            {
                var featureType = new FeatureType((int)tableStyleOption.Enum)
                {
                    Tag = "TableStyleOption",
                    Description = tableStyleOption.Description,
                    Tip = tableStyleOption.Tip,
                    IsSupported = true,
                    IsActive = tableStyleOption.Enum == Model.Settings.Importer.SelectedTableStyleOption,
                    SelectedCommand = new ActionCommand(() =>
                    {
                        Model.Settings.Importer.SelectedTableStyleOption = tableStyleOption.Enum;
                        FilterChanged.Execute(null);
                    })
                };

                return featureType;
            }).ToList();

            return featureTypes;
        }

        private IEnumerable<FeatureType> CreateTableMatchOptions()
        {
            // because matching is disabled, all tables will be unmatched
            if (!IsMatchingEnabled)
                Model.Settings.Importer.SelectedTableMatchOption = TableMatchOptionEnum.OnlineOnly;

            // all table match options
            var featureTypes = StaticSettings.TableMatchOptions.Select(tableMatchOption =>
            {
                var featureType = new FeatureType((int)tableMatchOption.Enum)
                {
                    Tag = "TableMatchOption",
                    Description = tableMatchOption.Description,
                    Tip = tableMatchOption.Tip,
                    IsSupported = tableMatchOption.Enum == TableMatchOptionEnum.OnlineOnly || IsMatchingEnabled,
                    IsActive = tableMatchOption.Enum == Model.Settings.Importer.SelectedTableMatchOption,
                    SelectedCommand = new ActionCommand(() =>
                    {
                        Model.Settings.Importer.SelectedTableMatchOption = tableMatchOption.Enum;
                        FilterChanged.Execute(null);
                    })
                };

                if (!IsMatchingEnabled && tableMatchOption.Enum != TableMatchOptionEnum.OnlineOnly)
                    featureType.Tip += MatchingDisabledMessage;

                return featureType;
            }).ToList();

            return featureTypes;
        }

        private void NavigateToBackupFolder() => Process.Start("explorer.exe", BackupFolder);

        private void UpdateIsNew()
        {
            // flag models if they satisfy the update time range
            var onlineGames = GetOnlineGames();
            onlineGames.ForEach(onlineGame => onlineGame.AllFiles.ForEach(kv =>
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

        private static void NavigateToUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

        private static void ShowImage(string tableImgUrl)
        {
            var imageUrlSelection = new UrlSelection
            {
                Url = tableImgUrl,
                SelectedCommand = new ActionCommand(() => DialogHost.Close("ImporterResultsDialog"))
            };

            DialogHost.Show(imageUrlSelection, "ImporterResultsDialog");
        }

        private IEnumerable<OnlineGame> GetOnlineGames() => GameItems.Where(item => item.OnlineGame != null).Select(item => item.OnlineGame);

        private readonly Regex _regexExtractIpdbId = new Regex(@"http.?:\/\/www\.ipdb\.org\/machine\.cgi\?id=(?<ipdbId>\d*)$", RegexOptions.Compiled);
        private readonly IList<GameDetail> _localGames;
        private readonly Dictionary<string, int> _matchStatistics;
        private const string DialogHostName = "ImporterResultsDialog";

        private const int WindowMargin = 0;
        private const string MatchingDisabledMessage = "... DISABLED BECAUSE MATCHING WASN'T ENABLED";
    }
}