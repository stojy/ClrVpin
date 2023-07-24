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
            
            // mark files that are eligible for the ignore filtering
            onlineGame.TableFiles.ForEach(tableFile =>
            {
                var comment = tableFile.Comment?.ToLower().Trim();
                tableFile.IsVirtualOnly = comment?.StartsWith("vr room") == true;
                tableFile.IsMusicOrSoundMod = comment?.ContainsAny("sound mod", "music mod")  == true;
            });

            onlineGame.B2SFiles.ForEach(backglassFile =>
            {
                backglassFile.IsFullDmd = backglassFile.Features?.Any(feature => feature?.ToLower().Trim() == "fulldmd") == true ||
                                          backglassFile.Urls?.Any(url => url.Url.ToLower().RemoveChars('-').Contains("fulldmd")) == true;
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
                (Settings.SelectedTableAvailabilityOption == TableAvailabilityOptionEnum.Any || game.OnlineGame?.TableAvailability == Settings.SelectedTableAvailabilityOption) &&
                (Settings.SelectedTableNewContentOption == TableNewContentOptionEnum.Any || game.OnlineGame?.NewContentType == Settings.SelectedTableNewContentOption) &&
                (Settings.SelectedTableMatchOption == TableMatchOptionEnum.All || game.TableMatchType == Settings.SelectedTableMatchOption) &&
                (Settings.SelectedTableStyleOptions.Contains(game.TableStyleOption.ToString())) &&
                (Settings.SelectedYearBeginFilter == null || string.CompareOrdinal(game.Year, 0, Settings.SelectedYearBeginFilter, 0, 50) >= 0) &&
                (Settings.SelectedYearEndFilter == null || string.CompareOrdinal(game.Year, 0, Settings.SelectedYearEndFilter, 0, 50) <= 0) &&
                (Settings.SelectedTypeFilter == null || string.CompareOrdinal(game.Type, 0, Settings.SelectedTypeFilter, 0, 50) == 0) &&
                (Settings.SelectedFormatFilter == null || game.OnlineGame?.TableFormats.Contains(Settings.SelectedFormatFilter) == true) &&
                // do we really need to re-filter against 'UpdatedAt' given it's already calculated during UpdateIsNew??
                (Settings.SelectedUpdatedAtDateBegin == null || game.UpdatedAt == null || game.UpdatedAt.Value >= Settings.SelectedUpdatedAtDateBegin) &&
                (Settings.SelectedUpdatedAtDateEnd == null || game.UpdatedAt == null || game.UpdatedAt.Value < Settings.SelectedUpdatedAtDateEnd.Value.AddDays(1)) &&
                
                (Settings.SelectedTableFilter == null || game.Name.Contains(Settings.SelectedTableFilter, StringComparison.OrdinalIgnoreCase)) &&
                (Settings.SelectedManufacturerFilter == null || game.Manufacturer.Contains(Settings.SelectedManufacturerFilter, StringComparison.OrdinalIgnoreCase)) &&
                game.OnlineGame?.AllFiles.Any(fileCollection => fileCollection.Value.IsNew) != false // keep if the online file collection is new or doesn't exist (i.e. unmatched)
        };
        GameItemsView.MoveCurrentToFirst();

        _gameCollections = new GameCollections(gameItems, () => GameFiltersViewModel?.UpdateFilterViews());

        DynamicFilteringCommand = new ActionCommand(() => RefreshViews(true));
        FilterChangedCommand = new ActionCommand(() => RefreshViews(Settings.IsDynamicFiltering));

        GameFiltersViewModel = new GameFiltersViewModel(GameItemsView, _gameCollections, Settings, () => FilterChangedCommand?.Execute(null))
        {
            TableMatchOptionsView = FeatureOptions.CreateFeatureOptionsSingleSelectionView(StaticSettings.TableMatchOptions, TableMatchOptionEnum.All,
                () => Model.Settings.Feeder.SelectedTableMatchOption, () => FilterChangedCommand.Execute(null)),
            TableAvailabilityOptionsView = FeatureOptions.CreateFeatureOptionsSingleSelectionView(StaticSettings.TableAvailabilityOptions, TableAvailabilityOptionEnum.Any,
                () => Model.Settings.Feeder.SelectedTableAvailabilityOption, () => FilterChangedCommand.Execute(null)),
            TableNewContentOptionsView = FeatureOptions.CreateFeatureOptionsSingleSelectionView(StaticSettings.TableNewContentOptions, TableNewContentOptionEnum.Any,
                () => Model.Settings.Feeder.SelectedTableNewContentOption, () => FilterChangedCommand.Execute(null)),
            IgnoreFeaturesOptionsView = FeatureOptions.CreateFeatureOptionsMultiSelectionView(StaticSettings.IgnoreFeatureOptions, () => Model.Settings.Feeder.SelectedIgnoreFeatureOptions,
                _ => UpdateIsNew(), includeSelectAll: false)
        };

        UpdatedFilterTimeChanged = new ActionCommand(UpdateIsNew);

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

    public string AddMissingDatabaseInfoTip { get; }
    public string OverwriteDatabaseInfoTip { get; }

    public string BackupFolder { get; }
    public ICommand NavigateToBackupFolderCommand { get; }

    public FeederSettings Settings { get; } = Model.Settings.Feeder;

    public ObservableCollection<GameItem> GameItems { get; }
    public ListCollectionView<GameItem> GameItemsView { get; }

    private readonly GameCollections _gameCollections;
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
            Height = (Model.ScreenWorkArea.Height - 10) * 0.73,
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

    private void UpdateIsNew()
    {
        // flag models if they satisfy the updated criteria
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

                    // treat file as NOT new if any of the except rules are satisfied
                    // VR only
                    if (file.IsNew && Settings.SelectedIgnoreFeatureOptions.Contains(IgnoreFeatureOptionEnum.VirtualRealityOnly) && file is TableFile tableFile) 
                        file.IsNew = !tableFile.IsVirtualOnly;

                    // music/sound mod
                    if (file.IsNew && Settings.SelectedIgnoreFeatureOptions.Contains(IgnoreFeatureOptionEnum.MusicOrSoundMod) && file is TableFile tableFile2) 
                        file.IsNew = !tableFile2.IsMusicOrSoundMod;

                    // full DMD
                    if (file.IsNew && Settings.SelectedIgnoreFeatureOptions.Contains(IgnoreFeatureOptionEnum.FullDmd) && file is ImageFile imageFile) 
                        file.IsNew = !imageFile.IsFullDmd;

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

        FilterChangedCommand.Execute(null);
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
    private const string DialogHostName = "FeederResultsDialog";

    private const int WindowMargin = 0;
    private const string MatchingDisabledMessage = ".. DISABLED BECAUSE MATCHING WASN'T ENABLED";
}