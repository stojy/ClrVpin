using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Feeder;
using ClrVpin.Logging;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using ClrVpin.Shared.FeatureType;
using ClrVpin.Shared.Utils;
using PropertyChanged;
using Utils;
using Utils.Extensions;
using ActionCommand = Microsoft.Xaml.Behaviors.Core.ActionCommand;

namespace ClrVpin.Explorer;

[AddINotifyPropertyChangedInterface]
public class ExplorerResultsViewModel
{
    public ExplorerResultsViewModel(IEnumerable<LocalGame> localGames)
    {
        Initialise(localGames);
    }

    public ExplorerSettings Settings { get; private set; }
    public ListCollectionView<GameItem> GameItemsView { get; private set; }

    public Window Window { get; private set; }

    public GameFiltersViewModel GameFiltersViewModel { get; private set; }
    public GameItem SelectedGameItem { get; set; }
    public ICommand LocalGameSelectedCommand { get; set; }
    public ICommand FilterChangedCommand { get; set; }
    public ICommand DynamicFilteringCommand { get; private set; }
    public ICommand MinRatingChangedCommand { get; set; }
    public ICommand MaxRatingChangedCommand { get; set; }

    public ICommand OverwriteDatabaseRomsCommand { get; private set; }
    public ICommand OverwriteDatabasePupsCommand { get;  private set;}

    public string BackupFolder { get; private set; }
    public ICommand NavigateToBackupFolderCommand { get; private set; }

    public ObservableCollection<GameItem> GameItems { get; private set; }

    public async Task Show(Window parentWindow, double left, double top, double width)
    {
        Window = new MaterialWindowEx
        {
            Owner = parentWindow,
            Title = "Results",
            Left = left,
            Top = top,
            Width = width,
            Height = (Model.ScreenWorkArea.Height - WindowMargin - WindowMargin) * 0.8,
            Content = this,
            Resources = parentWindow.Resources,
            ContentTemplate = parentWindow.FindResource("ExplorerResultsTemplate") as DataTemplate
        };
        Window.Show();

        await ShowSummary();
    }

    private void Initialise(IEnumerable<LocalGame> localGames)
    {
        Settings = Model.Settings.Explorer;

        // convert LocalGame to GameItem for consistency with Feeder, i.e. to make it easier for refactoring/sharing
        GameItems = new ObservableCollection<GameItem>(localGames.OrderBy(localGame => localGame.Game.Name).Select(localGame => new GameItem(localGame)));

        GameItems.ForEach(gameItem =>
        {
            // update status of each game, e.g. to update the Game.Content.UpdatedAt timestamp
            gameItem.LocalGame.Content.Update(() => new List<int>(), () => new List<int>());

            gameItem.LocalGame.UpdateDatabaseEntryCommand = new ActionCommand(() =>
                DatabaseItemManagement.UpdateDatabaseItem(DialogHostName, GameItems.Select(item => item.LocalGame).ToList(), gameItem, _gameCollections, () => GameItems.Remove(gameItem), false));
        });

        GameItemsView = new ListCollectionView<GameItem>(GameItems)
        {
            // filter the table names list to reflect the various view filtering criteria
            // - quickest checks placed first to short circuit evaluation of more complex checks
            Filter = gameItem =>
                (!Settings.SelectedTableStyleOptions.Any() || Settings.SelectedTableStyleOptions.Contains(gameItem.TableStyleOption.ToString())) &&
                (Settings.SelectedYearBeginFilter == null || string.CompareOrdinal(gameItem.Year, 0, Settings.SelectedYearBeginFilter, 0, 50) >= 0) &&
                (Settings.SelectedYearEndFilter == null || string.CompareOrdinal(gameItem.Year, 0, Settings.SelectedYearEndFilter, 0, 50) <= 0) &&
                (Settings.SelectedTypeFilter == null || string.CompareOrdinal(gameItem.Type, 0, Settings.SelectedTypeFilter, 0, 50) == 0) &&
                (Settings.SelectedUpdatedAtDateBegin == null || gameItem.UpdatedAt == null || gameItem.UpdatedAt.Value >= Settings.SelectedUpdatedAtDateBegin) &&
                (Settings.SelectedUpdatedAtDateEnd == null || gameItem.UpdatedAt == null || gameItem.UpdatedAt.Value < Settings.SelectedUpdatedAtDateEnd.Value.AddDays(1)) &&
                (Settings.SelectedTableFilter == null || gameItem.Name.Contains(Settings.SelectedTableFilter, StringComparison.OrdinalIgnoreCase)) &&
                (Settings.SelectedManufacturerFilter == null || gameItem.Manufacturer.Contains(Settings.SelectedManufacturerFilter, StringComparison.OrdinalIgnoreCase)) &&
                
                // if no missing file options are selected then the filter is effectively ignored
                (!Settings.SelectedMissingFileOptions.Any() || gameItem.LocalGame.Content.MissingImportantTypes.ContainsAny(Settings.SelectedMissingFileOptions)) &&

                // if no stale file options are selected then the filter is effectively ignored
                (!Settings.SelectedTableStaleOptions.Any() || 
                    gameItem.LocalGame.Content.IsTableVideoStale && Settings.SelectedTableStaleOptions.Contains(ContentTypeEnum.TableVideos) ||
                    gameItem.LocalGame.Content.IsBackglassVideoStale && Settings.SelectedTableStaleOptions.Contains(ContentTypeEnum.BackglassVideos)) &&

                // min rating match if either.. null selected min rating is a "don't care", but also explicitly handles no rating (i.e. null rating)
                // - game rating is null AND selected min rating is null
                // - game rating >= selected min rating, treating null as zero
                ((gameItem.Rating == null && Settings.SelectedMinRating == null) || gameItem.Rating >= (Settings.SelectedMinRating ?? 0)) &&
                // max rating match if either.. null selected max rating is a "don't care", no special 'no rating' is required as this is done during the min check
                // - game rating <= selected max rating, treating null as 5
                (gameItem.Rating ?? 0) <= (Settings.SelectedMaxRating ?? 5)
        };
        GameItemsView.MoveCurrentToFirst();

        _gameCollections = new GameCollections(GameItems, () => GameFiltersViewModel?.UpdateFilterViews());

        GameFiltersViewModel = new GameFiltersViewModel(GameItemsView, _gameCollections, Settings, () => FilterChangedCommand?.Execute(null));

        DynamicFilteringCommand = new ActionCommand(() => RefreshViews(true));
        FilterChangedCommand = new ActionCommand(() => RefreshViews(Settings.IsDynamicFiltering));
        MinRatingChangedCommand = new ActionCommand(MinRatingChanged);
        MaxRatingChangedCommand = new ActionCommand(MaxRatingChanged);

        GameFiltersViewModel.TableStyleOptionsView = FeatureOptions.CreateFeatureOptionsSelectionsView(StaticSettings.TableStyleOptions, Settings.SelectedTableStyleOptions,
            _ => FilterChangedCommand.Execute(null), includeSelectAll:false, minimumNumberOfSelections: 1);

        var missingFileOptions = Model.Settings.GetAllContentTypes().Where(x => x.Enum.In(StaticSettings.MissingFileOptions.Select(y => y.Enum))).ToArray();
        GameFiltersViewModel.MissingFilesOptionsView = FeatureOptions.CreateFeatureOptionsSelectionsView(missingFileOptions, Settings.SelectedMissingFileOptions, 
            _ => FilterChangedCommand.Execute(null), (enumOptions, enumOption) => (enumOptions.Cast<ContentType>().First(x => x == enumOption).IsFolderValid, Model.OptionsDisabledMessage));
        
        var tableStaleOptions = Model.Settings.GetAllContentTypes().Where(x => x.Enum.In(StaticSettings.TableStaleOptions.Select(y => y.Enum))).ToArray();
        GameFiltersViewModel.TableStaleOptionsView = FeatureOptions.CreateFeatureOptionsSelectionsView(tableStaleOptions, Settings.SelectedTableStaleOptions, 
            _ => FilterChangedCommand.Execute(null), (enumOptions, enumOption) => (enumOptions.Cast<ContentType>().First(x => x == enumOption).IsFolderValid, Model.OptionsDisabledMessage));

        BackupFolder = Model.Settings.BackupFolder;
        NavigateToBackupFolderCommand = new Utils.ActionCommand(NavigateToBackupFolder);

        OverwriteDatabaseRomsCommand = new ActionCommand(OverwriteDatabaseRoms);
        OverwriteDatabasePupsCommand = new ActionCommand(OverwriteDatabasePups);
    }

    private async void OverwriteDatabaseRoms()
    {
        await GetRoms();
    }

    private async void OverwriteDatabasePups()
    {
        await GetPups();
    }

    private async Task GetRoms()
    {
        var progress = new ProgressViewModel("Extracting ROM Names From The Table Scripts");
        progress.Show(Window);

        // extract ROMs
        var tableFiles = GameItems.Where(gameItem => gameItem.LocalGame.Content.Hits.Any(hit => hit.ContentTypeEnum == ContentTypeEnum.Tables && hit.IsPresent)).ToList();
        var tableFileDetails = tableFiles.Select(tableFile => new TableFileDetail(tableFile.LocalGame.Game.Type, tableFile.LocalGame.Content.Hits.First().Path));
        var roms = await TableRomUtils.GetRomsAsync(tableFileDetails, (file, rationComplete) => progress.Update(file, rationComplete));

        // update DB
        // - don't overwrite existing DB entries if the table parsing failed
        progress.Update("Updating Database");
        var gamesDictionary = tableFiles.ToDictionary(tableFile => tableFile.LocalGame.Game.Name, tableFile => tableFile.LocalGame.Game);
        var updatedGameCount = 0;
        roms.ForEach(rom =>
        {
            if (!gamesDictionary.TryGetValue(rom.file, out var game) || game.Rom == rom.name)
                return;
            
            gamesDictionary[rom.file].Rom = rom.name;
            updatedGameCount++;
        });
        
        if (updatedGameCount > 0)
            DatabaseUtils.WriteGamesToDatabase(gamesDictionary.Values);

        // display result
        progress.Close();
        var (isSuccess, detail) = CreateNamesStatistics("ROM", roms, updatedGameCount, "\n¹ Tables without ROM support, e.g. PM, EM and some SS without VPinMame");
        await (isSuccess ? Notification.ShowSuccess(DialogHostName, "Success", null, detail) : Notification.ShowWarning(DialogHostName, "Failed to Extract Some ROM Names", null, detail));
    }
    
    private async Task GetPups()
    {
        var progress = new ProgressViewModel("Extracting PuP Names From The Table Scripts");
        progress.Show(Window);

        // extract PuPs
        var tableFiles = GameItems.Where(gameItem => gameItem.LocalGame.Content.Hits.Any(hit => hit.ContentTypeEnum == ContentTypeEnum.Tables && hit.IsPresent)).ToList();
        var tableFileDetails = tableFiles.Select(tableFile => new TableFileDetail(null, tableFile.LocalGame.Content.Hits.First().Path));
        var pups = await TablePupUtils.GetPupsAsync(tableFileDetails, (file, rationComplete) => progress.Update(file, rationComplete));

        // update DB
        // - don't overwrite existing DB entries if the table parsing failed
        progress.Update("Updating Database");
        var gamesDictionary = tableFiles.ToDictionary(tableFile => tableFile.LocalGame.Game.Name, tableFile => tableFile.LocalGame.Game);
        var updatedGameCount = 0;
        pups.ForEach(pup =>
        {
            if (!gamesDictionary.TryGetValue(pup.file, out var game) || game.Pup == pup.name)
                return;
            
            gamesDictionary[pup.file].Pup = pup.name;
            updatedGameCount++;
        });
        
        if (updatedGameCount > 0)
            DatabaseUtils.WriteGamesToDatabase(gamesDictionary.Values);

        // display result
        progress.Close();
        var (isSuccess, detail) = CreateNamesStatistics("PuP", pups, updatedGameCount, "\n¹ Tables without PuP support");
        await (isSuccess ? Notification.ShowSuccess(DialogHostName, "Success", null, detail) : Notification.ShowWarning(DialogHostName, "Failed to Extract Some PuP Names", null, detail));
    }

    private void NavigateToBackupFolder() => Process.Start("explorer.exe", BackupFolder);

    private void MinRatingChanged()
    {
        ProcessRatingChanged(Settings.SelectedMinRating,
            roundedRating => Settings.SelectedMinRating = roundedRating,
            roundedRating => Settings.SelectedMaxRating = roundedRating);
    }

    private void MaxRatingChanged()
    {
        ProcessRatingChanged(Settings.SelectedMaxRating,
            roundedRating => Settings.SelectedMaxRating = roundedRating,
            roundedRating => Settings.SelectedMinRating = roundedRating);
    }

    private void ProcessRatingChanged(double? rating, Action<double?> setRating, Action<double?> setOtherRating)
    {
        // update rounding
        // - required because the underlying RatingsBar unfortunately doesn't bind the value to the 'ValueIncrements' used in the UI, e.g. bound value 1.456700001
        // - this will cause a synchronous property change to be processed if the rounded rating is different to the raw rating, which will be processed BEFORE this instance is completed
        var roundedRating = Rounding.ToHalf(rating);
        setRating(roundedRating);

        if (Settings.SelectedMinRating > Settings.SelectedMaxRating) // which also covers max < min
            setOtherRating(roundedRating);

        // only refresh the views if we're processing a rounded rating, i.e. skip the view refresh for the initial click which supplies a non-rounded rating
        if (roundedRating == null || roundedRating.Equals(rating))
        {
            // to ensure the ratings bar animation isn't interrupted (i.e. seen as stuttering) delay the view refresh a little
            // - required because the animation is CPU intensive and runs on the dispatcher (UI) thread
            RefreshViews(Settings.IsDynamicFiltering, 800);
        }
    }

    private void RefreshViews(bool refreshFilters, int? debounceMilliseconds = null)
    {
        // update main list
        GameItemsView.RefreshDebounce(debounceMilliseconds);

        // update filters based on what is shown in the main list
        if (refreshFilters)
            GameFiltersViewModel.Refresh(debounceMilliseconds);
    }

    private async Task ShowSummary()
    {
        var correctNameHits = GameItems.SelectMany(x => x.LocalGame.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitTypeEnum.CorrectName).ToList();

        var importantValidContentTypes = Model.Settings.GetAllValidContentTypes().Where(x => x.Enum.In(StaticSettings.ImportantContentTypes)).Select(x => x.Enum);
        var statistics = importantValidContentTypes.Select(contentType => CreateContentStatistics(correctNameHits, contentType)).ToList();

        var statisticsDetail = $"{"",-20}{"Missing", -16}{"Stale", -16}\n" + statistics.Select(x => $"{x.missingStatistic}").StringJoin("\n");
        
        // success requires no missing or stale files
        var missingCount = statistics.Sum(statistic => statistic.missingCount);
        var staleCount = statistics.Sum(statistic => statistic.staleCount);
        var isSuccess = missingCount  + staleCount == 0;

        await (isSuccess ? Notification.ShowSuccess(DialogHostName, "Important Files Are Up To Date") : Notification.ShowWarning(DialogHostName, "Missing or Stale Files Detected", null, statisticsDetail));
    }

    private (int missingCount, int? staleCount, string missingStatistic) CreateContentStatistics(IEnumerable<Hit> correctNameHits, ContentTypeEnum contentType)
    {
        var missingCount = GameItems.Count - correctNameHits.Count(hit => hit.ContentTypeEnum == contentType && hit.IsPresent);
        var missingStatistic = CreatePercentageStatistic(missingCount, GameItems.Count);

        var staleCount = contentType switch
        {
            ContentTypeEnum.TableVideos => GameItems.Count(gameItem => gameItem.LocalGame.Content.IsTableVideoStale),
            ContentTypeEnum.BackglassVideos => GameItems.Count(gameItem => gameItem.LocalGame.Content.IsBackglassVideoStale),
            _ => (int?)null
        };
        var staleStatistic = CreatePercentageStatistic(staleCount, GameItems.Count);

        var statistic = $"{contentType.GetDescription(),-20}{missingStatistic,-16}{staleStatistic,-16}";

        return (missingCount, staleCount, statistic);
    }

    private static (bool isSuccess, string detail) CreateNamesStatistics(string type, IReadOnlyCollection<(string path, bool? isSuccess, string name)> items, int updatedGameCount, string disclaimer)
    {
        var successCount = items.Count(item => item.isSuccess == true);
        var successDetail = CreateNamedPercentageStatistic("Successful Parsing", successCount, items.Count);

        var failedCount = items.Count(item => item.isSuccess == false);
        var failedDetail = CreateNamedPercentageStatistic("Failed Parsing", failedCount, items.Count);
        
        var skippedCount = items.Count(item => item.isSuccess == null);
        var skippedDetail = CreateNamedPercentageStatistic("n/a¹", skippedCount, items.Count);
        
        var updatedGameCountDetail = CreateNamedCountStatistic("Tables Updated", updatedGameCount);

        var detail = new[] { successDetail, failedDetail, skippedDetail, updatedGameCountDetail, disclaimer }.StringJoin("\n");

        Logger.Info($"{type} extraction: success={successCount}, failed={failedCount}, skipped={skippedCount}");

        return (failedCount == 0, detail);
    }

    private static string CreateNamedCountStatistic(string title, int count) => $"{title,-20} : {count}";
    
    private static string CreateNamedPercentageStatistic(string title, int count, int totalCount) => $"{title,-20} : {CreatePercentageStatistic(count, totalCount, true)}";

    private static string CreatePercentageStatistic(int? count, int totalCount, bool showTotalCount = false)
    {
        if (count == null)
            return "n/a";

        var missingPercentage = totalCount == 0 ? 0 : 100f * count / totalCount;
        var missingPercentageStatistic = $"{count,-4}{(showTotalCount ? $" of {totalCount}" : "")} ({missingPercentage:F2}%)";

        return missingPercentageStatistic;
    }

    private GameCollections _gameCollections;

    private const int WindowMargin = 0;
    private const string DialogHostName = "ResultsDialog";
}