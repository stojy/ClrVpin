using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Feeder;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
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

    public GameFiltersViewModel GameFilters { get; private set; }
    public GameItem SelectedGameItem { get; set; }
    public ICommand LocalGameSelectedCommand { get; set; }
    public ICommand FilterChangedCommand { get; set; }
    public ICommand DynamicFilteringCommand { get; private set; }
    public ICommand MinRatingChangedCommand { get; set; }
    public ICommand MaxRatingChangedCommand { get; set; }

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

        // ReSharper disable once UseObjectOrCollectionInitializer
        GameFilters = new GameFiltersViewModel(() => FilterChangedCommand?.Execute(null), startDate =>
        {
            Settings.SelectedUpdatedAtDateBegin = startDate;
            Settings.SelectedUpdatedAtDateEnd = DateTime.Today;
        });

        GameItems.ForEach(gameItem =>
        {
            // update status of each game, e.g. to update the Game.Content.UpdatedAt timestamp
            gameItem.LocalGame.Content.Update(() => new List<int>(), () => new List<int>());

            gameItem.LocalGame.ShowDetailedInfoCommand = new ActionCommand(() =>
                DatabaseItemManagement.UpdateDatabaseItem(DialogHostName, GameItems.Select(item => item.LocalGame).ToList(), gameItem, _gameCollections, () => GameItems.Remove(gameItem)));
        });

        GameItemsView = new ListCollectionView<GameItem>(GameItems)
        {
            // filter the table names list to reflect the various view filtering criteria
            // - quickest checks placed first to short circuit evaluation of more complex checks
            Filter = gameItem => (Settings.SelectedTableStyleOption == TableStyleOptionEnum.Both || gameItem.TableStyleOption == Settings.SelectedTableStyleOption) &&
                                 (Settings.SelectedYearBeginFilter == null || string.CompareOrdinal(gameItem.Year, 0, Settings.SelectedYearBeginFilter, 0, 50) >= 0) &&
                                 (Settings.SelectedYearEndFilter == null || string.CompareOrdinal(gameItem.Year, 0, Settings.SelectedYearEndFilter, 0, 50) <= 0) &&
                                 (Settings.SelectedTypeFilter == null || string.CompareOrdinal(gameItem.Type, 0, Settings.SelectedTypeFilter, 0, 50) == 0) &&
                                 (Settings.SelectedUpdatedAtDateBegin == null || gameItem.UpdatedAt == null || gameItem.UpdatedAt.Value >= Settings.SelectedUpdatedAtDateBegin) &&
                                 (Settings.SelectedUpdatedAtDateEnd == null || gameItem.UpdatedAt == null || gameItem.UpdatedAt.Value < Settings.SelectedUpdatedAtDateEnd.Value.AddDays(1)) &&
                                 (Settings.SelectedTableFilter == null || gameItem.Name.Contains(Settings.SelectedTableFilter, StringComparison.OrdinalIgnoreCase)) &&
                                 (Settings.SelectedManufacturerFilter == null || gameItem.Manufacturer.Contains(Settings.SelectedManufacturerFilter, StringComparison.OrdinalIgnoreCase)) &&

                                 // min rating match if either.. null selected min rating is a "don't care", but also explicitly handles no rating (i.e. null rating)
                                 // - game rating is null AND selected min rating is null
                                 // - game rating >= selected min rating, treating null as zero
                                 ((gameItem.Rating == null && Settings.SelectedMinRating == null) || gameItem.Rating >= (Settings.SelectedMinRating ?? 0)) &&
                                 // max rating match if either.. null selected max rating is a "don't care", no special 'no rating' is required as this is done during the min check
                                 // - game rating <= selected max rating, treating null as 5
                                 (gameItem.Rating ?? 0) <= (Settings.SelectedMaxRating ?? 5)
        };
        GameItemsView.MoveCurrentToFirst();

        _gameCollections = new GameCollections(GameItems, UpdateGameFilters);

        DynamicFilteringCommand = new ActionCommand(() => RefreshViews(true));
        FilterChangedCommand = new ActionCommand(() => RefreshViews(Settings.IsDynamicFiltering));
        MinRatingChangedCommand = new ActionCommand(MinRatingChanged);
        MaxRatingChangedCommand = new ActionCommand(MaxRatingChanged);

        UpdateGameFilters();
    }

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
        // - this will cause a synchronous property change to be processed, which will be processed BEFORE this instance is completed
        var roundedRating = Rounding.ToHalf(rating);
        setRating(roundedRating);

        if (Settings.SelectedMinRating > Settings.SelectedMaxRating) // which also covers max < min
            setOtherRating(roundedRating);

        // only refresh the views if we're processing a rounded rating, i.e. skip the view refresh for the initial click which supplies a non-rounded rating
        if (roundedRating?.Equals(rating) == true)
        {
            // to ensure the ratings bar animation isn't interrupted (i.e. seen as stuttering) delay the view refresh a little
            // - required because the animation is CPU intensive and runs on the dispatcher (UI) thread
            RefreshViews(Settings.IsDynamicFiltering, 800);
        }
    }

    private void UpdateGameFilters()
    {
        GameFilters.TableStyleOptionsView = FeatureOptions.CreateFeatureOptionsView(StaticSettings.TableStyleOptions, TableStyleOptionEnum.Manufactured,
            () => Settings.SelectedTableStyleOption, FilterChangedCommand);

        // filters views (drop down combo boxes)
        GameFilters.TablesFilterView = new ListCollectionView<string>(_gameCollections.TableNames)
        {
            // filter the table names list to reflect what's displayed in the localGames list, i.e. taking into account ALL of the existing filter criteria
            Filter = tableName => Filter(() => GameItemsView.Any(x => x.LocalGame.Game.Name == tableName))
        };

        GameFilters.ManufacturersFilterView = new ListCollectionView<string>(_gameCollections.Manufacturers)
        {
            // filter the manufacturers list to reflect what's displayed in the localGames list, i.e. taking into account ALL of the existing filter criteria
            Filter = manufacturer => Filter(() => GameItemsView.Any(x => x.LocalGame.Game.Manufacturer == manufacturer))
        };

        GameFilters.YearsBeginFilterView = new ListCollectionView<string>(_gameCollections.Years)
        {
            // filter the 'years from' list to reflect what's displayed in the localGames list, i.e. taking into account ALL of the existing filter criteria
            Filter = yearString => Filter(() => GameItemsView.Any(x => x.LocalGame.Game.Year == yearString))
        };
        GameFilters.YearsEndFilterView = new ListCollectionView<string>(_gameCollections.Years)
        {
            // filter the 'years to' list to reflect what's displayed in the localGames list, i.e. taking into account ALL of the existing filter criteria
            Filter = yearString => Filter(() => GameItemsView.Any(x => x.LocalGame.Game.Year == yearString))
        };

        // table HW type, i.e. SS, EM, PM
        GameFilters.TypesFilterView = new ListCollectionView<string>(_gameCollections.Types)
        {
            Filter = type => Filter(() => GameItemsView.Any(x => x.LocalGame.Game.Type == type))
        };
    }

    private bool Filter(Func<bool> dynamicFilteringFunc) =>
        // only evaluate the func if dynamic filtering is enabled
        !Settings.IsDynamicFiltering || dynamicFilteringFunc();

    private void RefreshViews(bool refreshFilters, int? debounceMilliseconds = null)
    {
        // update main list
        GameItemsView.RefreshDebounce(debounceMilliseconds);

        // update filters based on what is shown in the main list
        if (refreshFilters)
            GameFilters.Refresh(debounceMilliseconds);
    }

    private async Task ShowSummary()
    {
        var validHits = GameItems.SelectMany(x => x.LocalGame.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitTypeEnum.CorrectName).ToList();
        var eligibleFiles = GameItems.Count * Model.Settings.AllContentTypes.Count;
        var missingFilesCount = eligibleFiles - validHits.Count;

        var detail = CreatePercentageStatistic("Missing Files", missingFilesCount, eligibleFiles);
        var isSuccess = missingFilesCount == 0;

        await (isSuccess ? Notification.ShowSuccess(DialogHostName, "All Files Are Good") : Notification.ShowWarning(DialogHostName, "Missing or Incorrect Files", null, detail));
    }

    private static string CreatePercentageStatistic(string title, int count, int totalCount)
    {
        var percentage = totalCount == 0 ? 0 : 100f * count / totalCount;
        return $"{title}:  {count} of {totalCount} ({percentage:F2}%)";
    }

    private GameCollections _gameCollections;

    private const int WindowMargin = 0;
    private const string DialogHostName = "ResultsDialog";
}