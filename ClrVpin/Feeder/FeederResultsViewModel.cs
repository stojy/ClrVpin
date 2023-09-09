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
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Feeder.Vps;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared.Enums;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using ClrVpin.Shared.FeatureType;
using ClrVpin.Shared.Utils;
using MaterialDesignThemes.Wpf;
using PropertyChanged;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Feeder;

[AddINotifyPropertyChangedInterface]
public sealed class FeederResultsViewModel
{
    public FeederResultsViewModel(IList<GameItem> gameItems, IList<LocalGame> localGames)
    {
        // use the supplied localGames list instead of extracting from gameItems to ensure the existing ordering in the DB file(s) is preserved
        // - we don't want to re-order based on the online feed (after the various Feeder fixes) as this makes it too difficult to track the differences
        // - _localGames = gameItems.Where(item => item.LocalGame != null).Select(item => item.LocalGame).ToList();
        _localGames = localGames;

        IsMatchingEnabled = Model.Settings.Feeder.SelectedMatchCriteriaOptions.Any();

        UpdateGameItemsViewModel(gameItems);

        // main games view (data grid)
        GameItems = new ObservableCollection<GameItem>(gameItems);
        GameItemsView = new ListCollectionView<GameItem>(GameItems)
        {
            // game (~table) level filtering
            // - filter the top level games to reflect the various view filtering criteria
            // - quickest checks placed first to short circuit evaluation of more complex checks
            // - file level filtering (e.g. a game's table can have multiple files with different filtering properties) is also checked here.. but updated elsewhere, refer UpdateOnlineGameFileDetails
            Filter = GameItemsViewFilter
        };

        GameItemsView.MoveCurrentToFirst();

        _gameCollections = new GameCollections(gameItems, () => GameFiltersViewModel?.UpdateFilterViews());

        DynamicFilteringCommand = new ActionCommand(() => RefreshViews(true));
        FilterChangedCommand = new ActionCommand(() => RefreshViews(Settings.IsDynamicFiltering));

        GameFiltersViewModel = new GameFiltersViewModel(GameItemsView, _gameCollections, Settings, () => FilterChangedCommand?.Execute(null))
        {
            TableMatchOptionsView = FeatureOptions.CreateFeatureOptionsMultiSelectionView(StaticSettings.TableMatchOptions,
                () => Model.Settings.Feeder.SelectedTableMatchOptions, _ => UpdateOnlineGameFileDetails(), includeSelectAll: false, minimumNumberOfSelections: 1,
                isSupportedFunc: (_, enumOption) => (enumOption.Enum == TableMatchOptionEnum.OnlineOnly || IsMatchingEnabled, MatchingDisabledMessage)),

            UrlStatusOptionsView = FeatureOptions.CreateFeatureOptionsMultiSelectionView(StaticSettings.UrlStatusOptions,
                () => Model.Settings.Feeder.SelectedUrlStatusOptions, _ => UpdateOnlineGameFileDetails(), includeSelectAll: false, minimumNumberOfSelections: 1),

            // invoke online game file update to handle IsNewAndSelectedFileCollectionType which is file type sensitive
            OnlineFileTypeOptionsView = FeatureOptions.CreateFeatureOptionsMultiSelectionView(StaticSettings.OnlineFileTypeOptions,
                () => Model.Settings.Feeder.SelectedOnlineFileTypeOptions, _ => UpdateFilterOptionsBasedOnSelectedFileTypes(), includeSelectAll: false, minimumNumberOfSelections: 1),

            MiscFeaturesOptionsView = FeatureOptions.CreateFeatureOptionsMultiSelectionView(StaticSettings.MiscFeatureOptions,
                () => Model.Settings.Feeder.SelectedMiscFeatureOptions, _ => UpdateOnlineGameFileDetails(), includeSelectAll: false, minimumNumberOfSelections: 1),

            // simulator formats - vpx, fp, etc
            // - only applicable online
            SimulatorOptionsFilterView = FeatureOptions.CreateFeatureOptionsMultiSelectionView(StaticSettings.SimulatorOptions,
                () => Model.Settings.Feeder.SelectedSimulatorOptionFilter, _ => UpdateOnlineGameFileDetails(), includeSelectAll: false, minimumNumberOfSelections: 1)
        };

        // invoke online game file update to handle IsNew which is time sensitive
        UpdatedFilterTimeChanged = new ActionCommand(UpdateOnlineGameFileDetails);

        NavigateToUrlCommand = new ActionCommand<string>(url => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }));

        // update filter options and IsNew properties
        UpdateFilterOptionsBasedOnSelectedFileTypes();

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
            // - select the first tab item that has new file content AND matches the selected file type, e.g. tables, backglasses, etc.
            // - else, select the first tab that has file content
            // - else, select the first tab
            var selectedFileCollection = SelectedGameItem?.OnlineGame?.AllFileCollectionsList.FirstOrDefault(fileList => fileList.IsNewAndSelectedFileCollectionType) ??
                                         SelectedGameItem?.OnlineGame?.AllFileCollectionsList.FirstOrDefault(fileList => fileList.Count > 0) ??
                                         SelectedGameItem?.OnlineGame?.AllFileCollectionsList.First();

            // assign a convenience property to avoid a *lot* of nested referenced in the xaml
            SelectedOnlineGame = SelectedGameItem?.OnlineGame;
            SelectedFileCollection = selectedFileCollection;
        });

        // select the first item from the filtered list
        SelectedGameItem = GameItemsView.FirstOrDefault();
        GameItemSelectedCommand.Execute(null);
    }

    public string AddMissingDatabaseInfoTip { get; }
    public string OverwriteDatabaseInfoTip { get; }

    public string BackupFolder { get; }
    public ICommand NavigateToBackupFolderCommand { get; }

    public FeederSettings Settings { get; } = Model.Settings.Feeder;

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

    public GameFiltersViewModel GameFiltersViewModel { get; }

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
            Height = (Model.ScreenWorkArea.Height - 10) * 0.85,
            Content = this,
            Resources = parentWindow.Resources,
            ContentTemplate = parentWindow.FindResource("FeederResultsTemplate") as DataTemplate
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
            GameFiltersViewModel.Refresh();
    }

    private async Task ShowSummary()
    {
        if (!IsMatchingEnabled)
        {
            await Notification.ShowWarning(DialogHostName, "Reduced Functionality", "Because fuzzy matching was not enabled.");
            return;
        }

        // simplified summary of the FeederStatisticsViewModel info
        var restrictedGameItems = GameItems.Where(gameItem =>
            !gameItem.IsOriginal &&
            gameItem.OnlineGame?.TableFormats.Contains(SimulatorAbbreviationEnum.VirtualPinballX) == true &&
            gameItem.OnlineGame?.TableDownload == TableDownloadOptionEnum.Available).ToList();
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
                "1. Before starting, run Cleaner to confirm your collection is clean.\n" +
                "2. During the process, all the local database info is updated from online sources¹²³.\n" +
                "3. After completing, run Cleaner to re-clean your collection (e.g. rename files, delete obsolete files, etc).\n" +
                "\n" +
                "¹ The database file(s) are automatically backed up before any changes are made.\n" +
                "² Information that doesn't exist from online sources will not be overwritten (e.g. ratings)\n" +
                "³ In extreme cases, if your local database had substantially different values for 'name' and 'description',\n" +
                "  then Cleaner may not be able to automatically rename the files.  You can fix this by either..\n" +
                "  a. Run Cleaner with trainer wheels to identify the files, then manually rename the files.\n" +
                "  b. Run Cleaner without trainer wheels, then rename files (in the backup folder), then run Merger\n" +
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
            DatabaseUtils.WriteGamesToDatabase(_localGames.Select(x => x.Game));

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

    private void NavigateToBackupFolder() => Process.Start("explorer.exe", BackupFolder);

    private void UpdateGameItemsViewModel(IList<GameItem> gameItems)
    {
        // assign VM properties
        gameItems.ForEach(gameItem =>
        {
            // local database show/add commands
            gameItem.IsMatchingEnabled = IsMatchingEnabled;
            gameItem.UpdateDatabaseEntryCommand = new ActionCommand(() =>
                DatabaseItemManagement.UpdateDatabaseItem(DialogHostName, _localGames, gameItem, _gameCollections, () => GameItems?.Remove(gameItem), true));
            gameItem.CreateDatabaseEntryCommand = new ActionCommand(() =>
                DatabaseItemManagement.CreateDatabaseItem(DialogHostName, _localGames, gameItem, _gameCollections));
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

            // initialize VM attributes so they can be referenced quickly during filtering of the 'misc options'
            onlineGame.TableFiles.ForEach(tableFile =>
            {
                var comment = tableFile.Comment?.ToLower().Trim() ?? string.Empty;

                if (comment.ContainsAny("vr room", "vr standalone") && !comment.ContainsAny("optional vr room option", "vr room option"))
                    tableFile.FeatureOptions.Add(MiscFeatureOptionEnum.VirtualRealityOnly);

                if ((comment.ContainsAny("fss (full single screen)") && !comment.ContainsAny("fss (full single screen) option included"))
                    || tableFile.Urls.Select(u => u.Url).ContainsAny("https://fss-pinball.com"))
                    tableFile.FeatureOptions.Add(MiscFeatureOptionEnum.FullSingleScreenOnly);

                if (comment.ContainsAny("sound mod", "music mod", "jukebox mod"))
                    tableFile.FeatureOptions.Add(MiscFeatureOptionEnum.MusicOrSoundMod);

                if (comment.ContainsAny("bw mod", "black & white mod", "black and white mod"))
                    tableFile.FeatureOptions.Add(MiscFeatureOptionEnum.BlackAndWhiteMod);

                if (comment.ContainsAny("patch"))
                    tableFile.FeatureOptions.Add(MiscFeatureOptionEnum.Patch);

                // any table that doesn't contain at least one feature is considered as 'standard'
                if (tableFile.FeatureOptions.Count == 0)
                    tableFile.FeatureOptions.Add(MiscFeatureOptionEnum.Standard);

                tableFile.Simulator = SimulatorOptionHelper.GetEnum(tableFile.TableFormat);
            });

            onlineGame.B2SFiles.ForEach(backglassFile =>
            {
                if (backglassFile.Features?.Any(feature => feature?.ToLower().Trim() == "fulldmd") == true ||
                    backglassFile.Urls?.Any(url => url.Url.ToLower().RemoveChars('-').Contains("fulldmd")) == true)
                {
                    backglassFile.FeatureOptions.Add(MiscFeatureOptionEnum.FullDmd);
                }

                // any file that doesn't contain at least one feature is considered 'standard'
                if (backglassFile.FeatureOptions.Count == 0)
                    backglassFile.FeatureOptions.Add(MiscFeatureOptionEnum.Standard);
            });

            // extract IpdbId
            var match = _regexExtractIpdbId.Match(onlineGame.IpdbUrl ?? string.Empty);
            if (match.Success)
                onlineGame.IpdbId = match.Groups["ipdbId"].Value;

            // create the VPS URL
            // assign VPS Url (not a fix)
            onlineGame.VpsUrl = $"https://virtual-pinball-spreadsheet.web.app/game/{onlineGame.Id}";

            // update URL information
            onlineGame.AllFileCollections.Select(x => x.Value).SelectMany(x => x).ForEach(file =>
            {
                file.Urls.ForEach(url => url.SelectedCommand = new ActionCommand(() => NavigateToUrl(url.Url)));

                if (!file.Urls.Any())
                    file.UrlStatusEnum = UrlStatusEnum.Missing;
                else if (file.Urls.All(url => url.Broken))
                    file.UrlStatusEnum = UrlStatusEnum.Broken;
                else
                    file.UrlStatusEnum = UrlStatusEnum.Valid;
            });
        });
    }

    private void UpdateFilterOptionsBasedOnSelectedFileTypes()
    {
        // table file type - enable features
        UpdateMiscFeatureState(new[] { OnlineFileTypeEnum.Tables },
            MiscFeatureOptionEnum.VirtualRealityOnly, MiscFeatureOptionEnum.FullSingleScreenOnly, MiscFeatureOptionEnum.MusicOrSoundMod, MiscFeatureOptionEnum.BlackAndWhiteMod, MiscFeatureOptionEnum.Patch);

        // - enable simulator options and also default first option VPX if none are already selected
        var isTableEnabled = Settings.SelectedOnlineFileTypeOptions.Contains(OnlineFileTypeEnum.Tables.GetDescription());
        FeatureOptions.EnableDisableFeatureOptions(isTableEnabled, GameFiltersViewModel.SimulatorOptionsFilterView.ToList());
        if (isTableEnabled)
            FeatureOptions.SelectDefaultFeatureTypes(GameFiltersViewModel.SimulatorOptionsFilterView.ToList(), (int)SimulatorOptionEnum.VirtualPinballX, (int)SimulatorOptionEnum.Unknown);

        // backglass file - enable features
        UpdateMiscFeatureState(new[] { OnlineFileTypeEnum.Backglasses }, MiscFeatureOptionEnum.FullDmd);

        // table or backglass file - enable features
        var tableAndBackglassFileType = new[] { OnlineFileTypeEnum.Tables, OnlineFileTypeEnum.Backglasses };
        UpdateMiscFeatureState(tableAndBackglassFileType, MiscFeatureOptionEnum.Standard);

        // - automatically standard simulator options and also default first option VPX if none are already selected
        var isTableOrBackglassEnabled = Settings.SelectedOnlineFileTypeOptions.ContainsAny(tableAndBackglassFileType.Select(x => x.GetDescription()));
        if (isTableOrBackglassEnabled)
            FeatureOptions.SelectDefaultFeatureTypes(GameFiltersViewModel.MiscFeaturesOptionsView.ToList(), (int)MiscFeatureOptionEnum.Standard, (int)MiscFeatureOptionEnum.Standard);

        UpdateOnlineGameFileDetails();
    }

    private void UpdateMiscFeatureState(IEnumerable<OnlineFileTypeEnum> onlineFileTypes, params MiscFeatureOptionEnum[] miscFeatureOptionToUpdate)
    {
        var isFileTypeEnabled = Settings.SelectedOnlineFileTypeOptions.ContainsAny(onlineFileTypes.Select(x => x.GetDescription()));
        var miscFeatureOptions = miscFeatureOptionToUpdate.Select(x => (int)x);
        var miscFeatureOptionItems = GameFiltersViewModel.MiscFeaturesOptionsView.Where(option => miscFeatureOptions.Contains(option.Id));

        // update each of the relevant feature options
        FeatureOptions.EnableDisableFeatureOptions(isFileTypeEnabled, miscFeatureOptionItems.ToList());
    }

    private void UpdateOnlineGameFileDetails()
    {
        // file level filtering relies on IsNew (unlike higher/general filtering)
        // - for or all online games ONLY, iterate through the file types (e.g. table, backglass, etc) and flag every file(s) as new based on the file filtering options
        // - usage..
        //   a. gameItem filtering - top level filtering (online) gameItem filtering, e.g. if  selectedFileType=DMD then only display gameItem if at least one DMD file is new
        //   b. file type 'new badge'
        //   c. file highlight - outer green border
        //   d. file URL link - green button color
        var onlineGames = GetOnlineGames();
        onlineGames.ForEach(onlineGame =>
        {
            onlineGame.AllFileCollections.ForEach(kv =>
            {
                var (fileCollectionType, fileCollection) = kv;

                var fileCollectionTypeEnum = OnlineFileTypeHelper.GetEnum(fileCollectionType);

                fileCollection.ForEach(file =>
                {
                    // flag each FILE as new based on the various criteria
                    // - THIS IS THE ROOT IDENTIFIER OF WHICH ALL OTHER 'IS NEW' PROPERTIES ARE DERIVED
                    file.IsNew =
                        // update time range is satisfied
                        // - this is different to the generated 'gameItem updatedAt' which is an aggregation of the all the content and their file timestamps.. refer GameItemsView filtering
                        IsNewUpdatedTimestamp(file) &&

                        // feature options.. currently only supported by tables and backglasses
                        (!fileCollectionTypeEnum.In(OnlineFileTypeEnum.Tables, OnlineFileTypeEnum.Backglasses) ||
                         IsNew(fileCollectionTypeEnum, OnlineFileTypeEnum.Tables, () =>
                             file.FeatureOptions.ContainsAny(Settings.SelectedMiscFeatureOptions)) == true ||
                         IsNew(fileCollectionTypeEnum, OnlineFileTypeEnum.Backglasses, () =>
                             file.FeatureOptions.ContainsAny(Settings.SelectedMiscFeatureOptions)) == true
                        ) &&

                        // simulator application, aka file format, e.g. VPX, FP, etc.. only applies to tables, e.g. should be ignored for backglass, dmd, etc
                        (!fileCollectionTypeEnum.In(OnlineFileTypeEnum.Tables) ||
                         IsNew(fileCollectionTypeEnum, OnlineFileTypeEnum.Tables, () =>
                             Settings.SelectedSimulatorOptionFilter.Any() &&
                             Settings.SelectedSimulatorOptionFilter.Contains((file as TableFile)?.Simulator ?? SimulatorOptionEnum.Unknown)) == true) &&

                        // download URL status
                        IsNew(fileCollectionTypeEnum, null, () =>
                            Settings.SelectedUrlStatusOptions.Contains(file.UrlStatusEnum)) == true;

                    // url IsNew property - used to support download button color
                    file.Urls.ForEach(url => url.IsNew = file.IsNew);
                });

                fileCollection.Title = fileCollectionType;

                // fileCollection IsNew property - used to support *grey* 'new' icon on the file collection type shown on RHS, e.g. tables, backglasses, etc.
                fileCollection.IsNew = fileCollection.Any(file => file.IsNew);

                // fileCollection IsNew and content filter selected property - used to support *green* 'new' file collection and automatically selecting the relevant tab item
                fileCollection.IsNewAndSelectedFileCollectionType = fileCollection.IsNew && Settings.SelectedOnlineFileTypeOptions.Contains(fileCollectionType);
            });

            // support gameItem filtering, i.e. top level filter displayed on LHS
            // - assign a helper property to designate the new status of the file collections
            // - avoid re-calculating this every time we have a non-update time filter change
            onlineGame.IsNewFileCollectionTypes = onlineGame.AllFileCollections.Where(kv => kv.Value.IsNew).Select(kv => kv.Key).ToList();
        });

        FilterChangedCommand.Execute(null);
    }

    private bool GameItemsViewFilter(GameItem gameItem)
    {
        var show = (Settings.SelectedTableFilter == null || gameItem.Name.Contains(Settings.SelectedTableFilter, StringComparison.OrdinalIgnoreCase)) &&
                   // **ALL** file level filtering - this is based on IsNew calculated within UpdateOnlineGameFileDetails, e.g. FSS only, time range, simulator, url status, etc
                   // - exclude any gameItem that doesn't have new files for one of the selected content types
                   // - this also takes care of the individual file IsNew updates (e.g. VR only, sound mod, etc)
                   (gameItem.OnlineGame == null || Settings.SelectedOnlineFileTypeOptions.ContainsAny(gameItem.OnlineGame.IsNewFileCollectionTypes)) &&

                   // table match type, e.g. local, online, local and online
                   Settings.SelectedTableMatchOptions.Contains(gameItem.TableMatchType) &&

                   // manufacturer - original or manufactured
                   (!Settings.SelectedManufacturedOptions.Any() ||
                    (Settings.SelectedManufacturedOptions.Contains(YesNoNullableBooleanOptionEnum.True) && gameItem.TableStyleOption == TableStyleOptionEnum.Manufactured) ||
                    (Settings.SelectedManufacturedOptions.Contains(YesNoNullableBooleanOptionEnum.False) && gameItem.TableStyleOption == TableStyleOptionEnum.Original)) &&

                   // manufacture/construction date
                   (Settings.SelectedYearBeginFilter == null || string.CompareOrdinal(gameItem.Year, 0, Settings.SelectedYearBeginFilter, 0, 50) >= 0) &&
                   (Settings.SelectedYearEndFilter == null || string.CompareOrdinal(gameItem.Year, 0, Settings.SelectedYearEndFilter, 0, 50) <= 0) &&

                   // technology type, i.e. SS, EM, PM, unknown
                   (!Settings.SelectedTechnologyTypeOptions.Any() ||
                    Settings.SelectedTechnologyTypeOptions.Contains(gameItem.TechnologyType ?? TechnologyTypeOptionEnum.Unknown)) &&

                   // manufacture name
                   (Settings.SelectedManufacturerFilter == null || gameItem.Manufacturer.Contains(Settings.SelectedManufacturerFilter, StringComparison.OrdinalIgnoreCase));
        
        return show;
    }

    private static bool? IsNew(OnlineFileTypeEnum actualFileCollectionTypeEnum, OnlineFileTypeEnum? requiredOnlineFileTypeEnum, Func<bool> isNewFunc) =>
        // if the file type matches (or is don't care), then invoke the callback to allow the caller to determine if the file is new (or not)
        // - returning null indicates no check was made, i.e. n/a
        requiredOnlineFileTypeEnum == null || actualFileCollectionTypeEnum == requiredOnlineFileTypeEnum ? isNewFunc() : null;

    private bool IsNewUpdatedTimestamp(File file) => file.UpdatedAt >= (Settings.SelectedUpdatedAtDateBegin ?? DateTime.MinValue) && file.UpdatedAt <= (Settings.SelectedUpdatedAtDateEnd?.AddDays(1) ?? DateTime.Now);

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

    private readonly GameCollections _gameCollections;

    private readonly Regex _regexExtractIpdbId = new(@"http.?:\/\/www\.ipdb\.org\/machine\.cgi\?id=(?<ipdbId>\d*)$", RegexOptions.Compiled);
    private readonly IList<LocalGame> _localGames;
    private const string DialogHostName = "FeederResultsDialog";

    private const int WindowMargin = 0;
    private const string MatchingDisabledMessage = ".. DISABLED BECAUSE MATCHING WASN'T ENABLED";
}