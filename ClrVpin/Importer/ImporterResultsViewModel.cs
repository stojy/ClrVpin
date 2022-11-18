using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
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
    public interface IGameCollections
    {
        IList<string> Manufacturers { get; }
        IList<string> Types { get; }
        IList<string> Years { get; }
        IList<string> Players { get; }
        IList<string> Roms { get; }
        IList<string> Themes { get; }
        IList<string> Authors { get; }

        public void UpdateCollections();
    }

    [AddINotifyPropertyChangedInterface]
    public class ImporterResultsViewModel : IGameCollections
    {
        public ImporterResultsViewModel(IList<GameItem> gameItems, IList<LocalGame> localGames)
        {
            // use the supplied localGames list instead of extracting from gameItems to ensure the existing ordering in the DB file(s) is preserved
            // - we don't want to re-order based on the online feed (after the various importer fixes) as this makes it too difficult to track the differences
            // - _localGames = gameItems.Where(item => item.LocalGame != null).Select(item => item.LocalGame).ToList();
            _localGames = localGames;

            IsMatchingEnabled = Model.Settings.Importer.SelectedMatchCriteriaOptions.Any();

            TableStyleOptionsView = CreateFeatureOptionsView(StaticSettings.TableStyleOptions, TableStyleOptionEnum.Manufactured, () => Model.Settings.Importer.SelectedTableStyleOption);
            TableMatchOptionsView = CreateFeatureOptionsView(StaticSettings.TableMatchOptions, TableMatchOptionEnum.All, () => Model.Settings.Importer.SelectedTableMatchOption);
            TableAvailabilityOptionsView = CreateFeatureOptionsView(StaticSettings.TableAvailabilityOptions, TableAvailabilityOptionEnum.Both, () => Model.Settings.Importer.SelectedTableAvailabilityOption);
            TableNewContentOptionsView = CreateFeatureOptionsView(StaticSettings.TableNewContentOptions, TableNewContentOptionEnum.All, () => Model.Settings.Importer.SelectedTableNewContentOption);
            PresetDateOptionsView = CreatePresetDateOptionsView(StaticSettings.PresetDateOptions);

            // assign VM properties
            gameItems.ForEach(gameItem =>
            {
                // local database show/add commands
                gameItem.IsMatchingEnabled = IsMatchingEnabled;
                gameItem.UpdateDatabaseEntryCommand = new ActionCommand(() =>
                    DatabaseItemManagement.UpdateDatabaseItem(_localGames, gameItem, this, () => GameItems.Remove(gameItem)));
                gameItem.CreateDatabaseEntryCommand = new ActionCommand(() =>
                    DatabaseItemManagement.CreateDatabaseItem(_localGames, gameItem, this));
                gameItem.UpdateDatabaseMatchedEntryTooltip += IsMatchingEnabled ? "" : MatchingDisabledMessage;
                gameItem.UpdateDatabaseUnmatchedEntryTooltip += IsMatchingEnabled ? "" : MatchingDisabledMessage;
                gameItem.CreateDatabaseEntryTooltip += IsMatchingEnabled ? "" : MatchingDisabledMessage;

                if (gameItem.OnlineGame is not { } onlineGame) // pattern matching - assign AND check for not null!
                    return;

                // image - for showing dialog with larger view of image
                onlineGame.ImageUrlSelection = new UrlSelection
                {
                    Url = onlineGame.ImgUrl,
                    SelectedCommand = new ActionCommand(() => ShowImage(onlineGame.ImgUrl))
                };

                // show large image popup
                onlineGame.ImageFiles.ForEach(imageFile =>
                {
                    imageFile.ImageUrlSelection = new UrlSelection
                    {
                        Url = imageFile.ImgUrl,
                        SelectedCommand = new ActionCommand(() => ShowImage(imageFile.ImgUrl))
                    };
                });

                // extract IpdbId
                var match = _regexExtractIpdbId.Match(onlineGame.IpdbUrl ?? string.Empty);
                if (match.Success)
                    onlineGame.IpdbId = match.Groups["ipdbId"].Value;

                // create the VPS URL
                // assign VPS Url (not a fix)
                onlineGame.VpsUrl = $@"https://virtual-pinball-spreadsheet.web.app/game/{onlineGame.Id}";


                // navigate to url
                onlineGame.AllFiles.Select(x => x.Value).SelectMany(x => x).ForEach(file => { file.Urls.ForEach(url => url.SelectedCommand = new ActionCommand(() => NavigateToUrl(url.Url))); });
            });

            // main games view (data grid)
            GameItems = new ObservableCollection<GameItem>(gameItems);
            GameItemsView = new ListCollectionView<GameItem>(GameItems)
            {
                // filter the table names list to reflect the various view filtering criteria
                // - quickest checks placed first to short circuit evaluation of more complex checks
                Filter = game =>
                    (Settings.SelectedTableAvailabilityOption == TableAvailabilityOptionEnum.Both || game.OnlineGame?.TableAvailability == Settings.SelectedTableAvailabilityOption) &&
                    (Settings.SelectedTableNewContentOption == TableNewContentOptionEnum.All || game.OnlineGame?.NewContentType == Settings.SelectedTableNewContentOption) &&
                    (Settings.SelectedTableMatchOption == TableMatchOptionEnum.All || Settings.SelectedTableMatchOption == game.TableMatchType) &&
                    (Settings.SelectedTableStyleOption == TableStyleOptionEnum.Both || Settings.SelectedTableStyleOption == game.TableStyleOption) &&
                    (Settings.SelectedYearBeginFilter == null || string.CompareOrdinal(game.OnlineGame?.YearString, 0, Settings.SelectedYearBeginFilter, 0, 50) >= 0) &&
                    (Settings.SelectedYearEndFilter == null || string.CompareOrdinal(game.OnlineGame?.YearString, 0, Settings.SelectedYearEndFilter, 0, 50) <= 0) &&
                    (Settings.SelectedTypeFilter == null || string.CompareOrdinal(Settings.SelectedTypeFilter, 0, game.Type, 0, 50) == 0) &&
                    (Settings.SelectedFormatFilter == null || game.OnlineGame?.TableFormats.Contains(Settings.SelectedFormatFilter) == true) &&
                    (Settings.SelectedUpdatedAtDateBegin == null || game.UpdatedAt == null || game.UpdatedAt.Value >= Settings.SelectedUpdatedAtDateBegin) &&
                    (Settings.SelectedUpdatedAtDateEnd == null || game.UpdatedAt == null || game.UpdatedAt.Value < Settings.SelectedUpdatedAtDateEnd.Value.AddDays(1)) &&
                    (Settings.SelectedTableFilter == null || game.Name.Contains(Settings.SelectedTableFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (Settings.SelectedManufacturerFilter == null || game.Manufacturer.Contains(Settings.SelectedManufacturerFilter, StringComparison.OrdinalIgnoreCase))
            };
            GameItemsView.MoveCurrentToFirst();

            // create game collections (e.g. list of manufacturers) to be used by results and also externally by the database item dialogs
            UpdateCollections();

            DynamicFilteringCommand = new ActionCommand(() => RefreshViews(true));
            FilterChangedCommand = new ActionCommand(() => RefreshViews(Settings.IsDynamicFiltering));

            UpdatedFilterTimeChanged = new ActionCommand(() =>
            {
                UpdateIsNew();
                FilterChangedCommand.Execute(null);
            });

            NavigateToUrlCommand = new ActionCommand<string>(url => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }));

            // force an 'update filter time' change so that the correct 'IsNew' values are calculated
            UpdatedFilterTimeChanged.Execute(null);

            BackupFolder = Model.Settings.BackupFolder;
            NavigateToBackupFolderCommand = new ActionCommand(NavigateToBackupFolder);

            AddMissingDatabaseInfoTip += "Add any missing information in your local database from online sources" + (IsMatchingEnabled ? "" : MatchingDisabledMessage);
            AllTableAddMissingDatabaseInfoCommand = new ActionCommand(AllTableAddMissingDatabaseProperties);

            OverwriteDatabaseInfoTip += "Overwrite all information in your local database from online sources. Information that doesn't exist from online sources will not be overwritten (e.g. ratings)." +
                                        (IsMatchingEnabled ? "" : MatchingDisabledMessage);
            AllTableOverwriteDatabaseInfoCommand = new ActionCommand(AllTableOverwriteDatabaseProperties);

            GameItemSelectedCommand = new ActionCommand(() =>
            {
                // re-assign the selected tab item when the selected game is changed, priority order..
                // - select the first tab item that has new file content, e.g. tables, backglasses, etc.
                // - else, select the first tab that has file content
                // - else, select the first tab
                var selectedFileCollection = SelectedGameItem?.OnlineGame?.AllFilesList.FirstOrDefault(fileList => fileList.IsNew) ??
                                             SelectedGameItem?.OnlineGame?.AllFilesList.FirstOrDefault(fileList => fileList.Count > 0) ??
                                             SelectedGameItem?.OnlineGame?.AllFilesList.First();

                // assign a convenience property to avoid a *lot* of nested referenced in the xaml
                SelectedOnlineGame = SelectedGameItem?.OnlineGame;
                SelectedFileCollection = selectedFileCollection;
            });

            // select the first item from the filtered list
            SelectedGameItem = GameItemsView.FirstOrDefault();
            GameItemSelectedCommand.Execute(null);
        }

        public void UpdateCollections()
        {
            // the collections consist of all the possible permutations from BOTH the online source and the local source
            // - this is to ensure the maximum possible options are presented AND that the active item (from the local DB in the case of the update dialog) is actually in the list,
            //   otherwise it will be assigned to null via the ListCollectionView when the SelectedItem is assigned (either explicitly or via binding)

            // filters views (drop down combo boxes) - uses the online AND unmatched local DB 
            var tableNames = GameItems.Select(x => x.Names).SelectManyUnique();
            TablesFilterView = new ListCollectionView<string>(tableNames)
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = table => Filter(() => GameItemsView.Any(x => x.Name == table))
            };

            Manufacturers = GameItems.Select(x => x.Manufacturers).SelectManyUnique();
            ManufacturersFilterView = new ListCollectionView<string>(Manufacturers)
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = manufacturer => Filter(() => GameItemsView.Any(x => x.Manufacturer == manufacturer))
            };

            Years = GameItems.Select(x => x.Years).SelectManyUnique();
            YearsBeginFilterView = new ListCollectionView<string>(Years)
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = yearString => Filter(() => GameItemsView.Any(x => x.Year == yearString))
            };
            YearsEndFilterView = new ListCollectionView<string>(Years)
            {
                // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
                Filter = yearString => Filter(() => GameItemsView.Any(x => x.Year == yearString))
            };

            // table HW type, i.e. SS, EM, PM
            Types = GameItems.Select(x => x.Types).SelectManyUnique();
            TypesFilterView = new ListCollectionView<string>(Types)
            {
                Filter = type => Filter(() => GameItemsView.Any(x => x.Type == type))
            };

            // table formats - vpx, fp, etc
            // - only available via online
            Formats = GameItems.SelectMany(x => x.OnlineGame?.TableFormats ?? new List<string>()).Distinct().Where(x => x != null).OrderBy(x => x).ToList();
            FormatsFilterView = new ListCollectionView<string>(Formats)
            {
                Filter = format => Filter(() => GameItemsView.Any(x => x.OnlineGame?.TableFormats.Contains(format) == true))
            };

            Themes = GameItems.Select(x => x.Themes).SelectManyUnique();
            Players = GameItems.Select(x => x.Players).SelectManyUnique();
            Roms = GameItems.Select(x => x.Roms).SelectManyUnique();
            Authors = GameItems.Select(x => x.Authors).SelectManyUnique();
        }

        private bool Filter(Func<bool> dynamicFilteringFunc)
        {
            // only evaluate the func if dynamic filtering is enabled
            return !Settings.IsDynamicFiltering || dynamicFilteringFunc();
        }

        public string AddMissingDatabaseInfoTip { get; }
        public string OverwriteDatabaseInfoTip { get; }

        public ListCollectionView<FeatureType> TableStyleOptionsView { get; }
        public ListCollectionView<FeatureType> TableMatchOptionsView { get; }
        public ListCollectionView<FeatureType> TableAvailabilityOptionsView { get; }
        public ListCollectionView<FeatureType> TableNewContentOptionsView { get; }
        public ListCollectionView<FeatureType> PresetDateOptionsView { get; }

        public string BackupFolder { get; }
        public ICommand NavigateToBackupFolderCommand { get; }

        public ImporterSettings Settings { get; } = Model.Settings.Importer;

        // todo; move filters into a separate class?
        public ListCollectionView<string> TablesFilterView { get; private set; }
        public ListCollectionView<string> ManufacturersFilterView { get; private set;}
        public ListCollectionView<string> YearsBeginFilterView { get; private set;}
        public ListCollectionView<string> YearsEndFilterView { get; private set;}
        public ListCollectionView<string> TypesFilterView { get; private set;}
        public ListCollectionView<string> FormatsFilterView { get; private set;}

        public ObservableCollection<GameItem> GameItems { get; }
        public ListCollectionView<GameItem> GameItemsView { get; }

        public Window Window { get; private set; }

        public GameItem SelectedGameItem { get; set; }
        public OnlineGame SelectedOnlineGame { get; private set; }

        public ICommand DynamicFilteringCommand { get; }
        public ICommand FilterChangedCommand { get; set; }
        public ICommand UpdatedFilterTimeChanged { get; set; }

        public ICommand NavigateToUrlCommand { get; }
        public ICommand AllTableAddMissingDatabaseInfoCommand { get; }
        public ICommand AllTableOverwriteDatabaseInfoCommand { get; }
        public ICommand GameItemSelectedCommand { get; }
        
        public bool IsMatchingEnabled { get; }
        public FileCollection SelectedFileCollection { get; set; }

        // IGameCollections
        public IList<string> Manufacturers { get; private set; }
        public IList<string> Types { get; private set;}
        private IList<string> Formats { get; set;}
        public IList<string> Years { get; private set;}
        public IList<string> Players { get; private set;}
        public IList<string> Roms { get; private set;}
        public IList<string> Themes { get; private set;}
        public IList<string> Authors { get; private set;}

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

        private void RefreshViews(bool refreshFilters)
        {
            // update main list
            GameItemsView.RefreshDebounce();

            // update filters based on what is shown in the main list
            if (refreshFilters)
            {
                TablesFilterView.RefreshDebounce();
                ManufacturersFilterView.RefreshDebounce();
                YearsBeginFilterView.RefreshDebounce();
                YearsEndFilterView.RefreshDebounce();
                TypesFilterView.RefreshDebounce();
                FormatsFilterView.RefreshDebounce();
            }
        }

        private async Task ShowSummary()
        {
            if (!IsMatchingEnabled)
            {
                await Notification.ShowWarning(DialogHostName, "Reduced Functionality", "Because fuzzy matching was not enabled.");
                return;
            }

            // simplified summary of the ImporterStatisticsViewModel info
            var restrictedGameItems = GameItems.Where(gameItem => 
                !gameItem.IsOriginal && 
                gameItem.OnlineGame?.TableFormats.Contains("VPX") == true && 
                gameItem.OnlineGame?.TableAvailability == TableAvailabilityOptionEnum.Available).ToList();
            var matchedManufacturedCount = restrictedGameItems.Count(gameItem => gameItem.TableMatchType is TableMatchOptionEnum.LocalAndOnline);
            var missingManufacturedCount = restrictedGameItems.Count(gameItem => gameItem.TableMatchType is TableMatchOptionEnum.OnlineOnly);

            var detail = CreatePercentageStatistic("Missing Manufactured Tables", missingManufacturedCount, missingManufacturedCount + matchedManufacturedCount);

            var isSuccess = missingManufacturedCount == 0;
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
            // - updated properties via OnlineGames.Hit.LocalGame are reflected in the local game entries
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

        private ListCollectionView<FeatureType> CreateFeatureOptionsView<T>(IEnumerable<EnumOption<T>> enumOptions, T highlightedOption, Expression<Func<T>> selectionExpression) where T : Enum
        {
            var accessor = new Accessor<T>(selectionExpression);

            // all table style options
            var featureTypes = enumOptions.Select(option =>
            {
                var featureType = new FeatureType(Convert.ToInt32(option.Enum))
                {
                    Tag = typeof(T).Name,
                    Description = option.Description,
                    Tip = option.Tip,
                    IsSupported = true,
                    IsHighlighted = option.Enum.IsEqual(highlightedOption),
                    IsActive = option.Enum.IsEqual(accessor.Get()),
                    SelectedCommand = new ActionCommand(() =>
                    {
                        accessor.Set(option.Enum);
                        FilterChangedCommand.Execute(null);
                    })
                };

                return featureType;
            }).ToList();

            return new ListCollectionView<FeatureType>(featureTypes);
        }
        
        private ListCollectionView<FeatureType> CreatePresetDateOptionsView(IEnumerable<EnumOption<PresetDateOptionEnum>> enumOptions)
        {
            // all preset date options
            var featureTypes = enumOptions.Select(option =>
            {
                var featureType = new FeatureType(Convert.ToInt32(option.Enum))
                {
                    Tag = nameof(PresetDateOptionEnum),
                    Description = option.Description,
                    Tip = option.Tip,
                    IsSupported = true,
                    SelectedCommand = new ActionCommand(() =>
                    {
                        // assign the updated at from begin date
                        var offset = option.Enum switch
                        {
                            PresetDateOptionEnum.Today => (0, 0),
                            PresetDateOptionEnum.Yesterday => (1, 0),
                            PresetDateOptionEnum.LastThreeDays => (3,0),
                            PresetDateOptionEnum.LastWeek => (7, 0),
                            PresetDateOptionEnum.LastMonth => (0,1),
                            PresetDateOptionEnum.LastThreeMonths => (0,3),
                            PresetDateOptionEnum.LastYear => (0,12),
                            _ => (0,0)
                        };
                        Settings.SelectedUpdatedAtDateBegin = DateTime.Today.AddDays(-offset.Item1).AddMonths(-offset.Item2);

                        FilterChangedCommand.Execute(null);
                    })
                };
                return featureType;
            }).ToList();

            return new ListCollectionView<FeatureType>(featureTypes);
        }

        private void NavigateToBackupFolder() => Process.Start("explorer.exe", BackupFolder);

        private void UpdateIsNew()
        {
            // flag models if they satisfy the update time range
            var onlineGames = GetOnlineGames();
            onlineGames.ForEach(onlineGame =>
            {
                onlineGame.AllFiles.ForEach(kv =>
                {
                    var (type, files) = kv;
                    files.ForEach(file =>
                    {
                        // flag file - if the update time range is satisfied
                        file.IsNew = file.UpdatedAt >= (Settings.SelectedUpdatedAtDateBegin ?? DateTime.MinValue) && file.UpdatedAt <= (Settings.SelectedUpdatedAtDateEnd?.AddDays(1) ?? DateTime.Now);

                        // flag each url within the file - required to allow for simpler view binding
                        file.Urls.ForEach(url => url.IsNew = file.IsNew);
                    });

                    // flag file collection info
                    files.IsNew = files.Any(file => file.IsNew);
                    files.Title = type;
                });

                // assign a helper property to designate games 'new content type', i.e. avoid re-calculating this every time we have a non-update time filter change
                if (onlineGame.AllFiles.Any(kv => kv.Value.IsNew))
                {
                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (onlineGame.AllFiles.Where(kv => kv.Key.In("Tables", "Backglasses", "DMDs")).Any(kv => kv.Value.IsNew))
                        onlineGame.NewContentType = TableNewContentOptionEnum.TableBackglassDmd;
                    else
                        onlineGame.NewContentType = TableNewContentOptionEnum.Other;
                }
                else
                {
                    // this game has no new content within the given time range
                    onlineGame.NewContentType = null;
                }
            });
        }

        private static void NavigateToUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

        private static async void ShowImage(string tableImgUrl)
        {
            var imageUrlSelection = new UrlSelection
            {
                Url = tableImgUrl,
                SelectedCommand = new ActionCommand(() =>
                {
                    if (DialogHost.IsDialogOpen(DialogHostName))
                        DialogHost.Close(DialogHostName);
                })
            };
            
            if (!DialogHost.IsDialogOpen(DialogHostName))
                await DialogHost.Show(imageUrlSelection, DialogHostName);
        }

        private IEnumerable<OnlineGame> GetOnlineGames() => GameItems.Where(item => item.OnlineGame != null).Select(item => item.OnlineGame);

        private readonly Regex _regexExtractIpdbId = new(@"http.?:\/\/www\.ipdb\.org\/machine\.cgi\?id=(?<ipdbId>\d*)$", RegexOptions.Compiled);
        private readonly IList<LocalGame> _localGames;
        public const string DialogHostName = "ImporterResultsDialog";

        private const int WindowMargin = 0;
        private const string MatchingDisabledMessage = "... DISABLED BECAUSE MATCHING WASN'T ENABLED";
    }
}