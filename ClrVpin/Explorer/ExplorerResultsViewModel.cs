using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Controls;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Explorer;

[AddINotifyPropertyChangedInterface]
public class ExplorerResultsViewModel
{
    public ExplorerResultsViewModel(ObservableCollection<LocalGame> games)
    {
        Games = games;
        
        Initialise();
    }

    public ExplorerSettings Settings { get; private set; }
    public ListCollectionView<LocalGame> GamesView { get; private set; }

    public Window Window { get; private set; }

    public ObservableCollection<LocalGame> Games { get; }

    public GameFiltersViewModel GameFilters { get; private set; }
    public LocalGame SelectedLocalGame { get; set; }
    public ICommand LocalGameSelectedCommand { get; set; }
    public ICommand FilterChangedCommand { get; set; }
    public ICommand DynamicFilteringCommand { get; private set; }

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


    private void Initialise()
    {
        Settings = Model.Settings.Explorer;

        // update status of each game, e.g. to update the Game.Content.UpdatedAt timestamp
        Games.ForEach(game => game.Content.Update(() => new List<int>(), () => new List<int>()));

        GamesView = new ListCollectionView<LocalGame>(Games)
        {
            // filter the table names list to reflect the various view filtering criteria
            // - quickest checks placed first to short circuit evaluation of more complex checks
            Filter = localGame =>
                (Settings.SelectedTableStyleOption == TableStyleOptionEnum.Both || localGame.Derived.TableStyleOption == Settings.SelectedTableStyleOption) &&
                (Settings.SelectedYearBeginFilter == null || string.CompareOrdinal(localGame.Game.Year, 0, Settings.SelectedYearBeginFilter, 0, 50) >= 0) &&
                (Settings.SelectedYearEndFilter == null || string.CompareOrdinal(localGame.Game.Year, 0, Settings.SelectedYearEndFilter, 0, 50) <= 0) &&
                (Settings.SelectedTypeFilter == null || string.CompareOrdinal(localGame.Game.Type, 0, Settings.SelectedTypeFilter, 0, 50) == 0) &&
                (Settings.SelectedUpdatedAtDateBegin == null || localGame.Content.UpdatedAt == null || localGame.Content.UpdatedAt.Value >= Settings.SelectedUpdatedAtDateBegin) &&
                (Settings.SelectedUpdatedAtDateEnd == null || localGame.Content.UpdatedAt == null || localGame.Content.UpdatedAt.Value < Settings.SelectedUpdatedAtDateEnd.Value.AddDays(1)) &&
                (Settings.SelectedTableFilter == null || localGame.Game.Name.Contains(Settings.SelectedTableFilter, StringComparison.OrdinalIgnoreCase)) &&
                (Settings.SelectedManufacturerFilter == null || localGame.Game.Manufacturer.Contains(Settings.SelectedManufacturerFilter, StringComparison.OrdinalIgnoreCase))
        };
        GamesView.MoveCurrentToFirst();

        DynamicFilteringCommand = new ActionCommand(() => RefreshViews(true));
        FilterChangedCommand = new ActionCommand(() => RefreshViews(Settings.IsDynamicFiltering));

        InitialiseFilters();

        //_allContentFeatureTypes = CreateAllContentFeatureTypes();
        //AllContentFeatureTypesView = new ListCollectionView<FeatureType>(_allContentFeatureTypes.ToList());

        //_allHitFeatureTypes = CreateAllHitFeatureTypes();
        //AllHitFeatureTypesView = new ListCollectionView<FeatureType>(_allHitFeatureTypes.ToList());

        //SearchTextCommand = new ActionCommand(SearchTextChanged);
        //ExpandGamesCommand = new ActionCommand<bool>(ExpandItems);

        //BackupFolder = FileUtils.ActiveBackupFolder;
        //NavigateToBackupFolderCommand = new ActionCommand(NavigateToBackupFolder);

        //UpdateStatus(Games);
        //InitView();
    }


    private void InitialiseFilters()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        GameFilters = new GameFiltersViewModel(() => FilterChangedCommand?.Execute(null), startDate =>
        {
            Settings.SelectedUpdatedAtDateBegin = startDate;
            Settings.SelectedUpdatedAtDateEnd = DateTime.Today;
        });

        GameFilters.TableStyleOptionsView = FeatureOptions.CreateFeatureOptionsView(StaticSettings.TableStyleOptions, TableStyleOptionEnum.Manufactured,
            () => Settings.SelectedTableStyleOption, FilterChangedCommand);


        // filters views (drop down combo boxes)
        var tableNames = Games.Select(x => x.Game.Name).SelectUnique();
        GameFilters.TablesFilterView = new ListCollectionView<string>(tableNames)
        {
            // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
            Filter = tableName => Filter(() => GamesView.Any(x => x.Game.Name == tableName))
        };

        var manufacturers = Games.Select(x => x.Game.Manufacturer).SelectUnique();
        GameFilters.ManufacturersFilterView = new ListCollectionView<string>(manufacturers)
        {
            // filter the manufacturers list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
            Filter = manufacturer => Filter(() => GamesView.Any(x => x.Game.Manufacturer == manufacturer))
        };

        var years = Games.Select(x => x.Game.Year).SelectUnique();
        GameFilters.YearsBeginFilterView = new ListCollectionView<string>(years)
        {
            // filter the 'years from' list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
            Filter = yearString => Filter(() => GamesView.Any(x => x.Game.Year == yearString))
        };
        GameFilters.YearsEndFilterView = new ListCollectionView<string>(years)
        {
            // filter the 'years to' list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
            Filter = yearString => Filter(() => GamesView.Any(x => x.Game.Year == yearString))
        };

        // table HW type, i.e. SS, EM, PM
        var types = Games.Select(x => x.Game.Type).SelectUnique();
        GameFilters.TypesFilterView = new ListCollectionView<string>(types)
        {
            Filter = type => Filter(() => GamesView.Any(x => x.Game.Type == type))
        };

        //// table formats - vpx, fp, etc
        //// - only available via online
        //    Formats = GameItems.SelectMany(x => x.OnlineGame?.TableFormats ?? new List<string>()).Distinct().Where(x => x != null).OrderBy(x => x).ToList();
        //    FormatsFilterView = new ListCollectionView<string>(Formats)
        //    {
        //        Filter = format => Filter(() => GameItemsView.Any(x => x.OnlineGame?.TableFormats.Contains(format) == true))
        //    };

        //    Themes = GameItems.Select(x => x.Themes).SelectManyUnique();
        //    Players = GameItems.Select(x => x.Players).SelectManyUnique();
        //Roms = GameItems.Select(x => x.Roms).SelectManyUnique();
        //    Authors = GameItems.Select(x => x.Authors).SelectManyUnique();
    }

    private bool Filter(Func<bool> dynamicFilteringFunc)
    {
        // only evaluate the func if dynamic filtering is enabled
        return !Settings.IsDynamicFiltering || dynamicFilteringFunc();
    }

    private void RefreshViews(bool refreshFilters)
    {
        // update main list
        GamesView.RefreshDebounce();

        // update filters based on what is shown in the main list
        if (refreshFilters)
            GameFilters.Refresh();
    }

    private async Task ShowSummary()
    {
        var validHits = Games.SelectMany(x => x.Content.ContentHitsCollection).SelectMany(x => x.Hits).Where(x => x.Type == HitTypeEnum.CorrectName).ToList();
        var eligibleFiles = Games.Count * Model.Settings.AllContentTypes.Count;
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

    private const int WindowMargin = 0;

    private const string DialogHostName = "ResultsDialog";
}