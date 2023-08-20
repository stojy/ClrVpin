using System;
using System.Collections.ObjectModel;
using System.Linq;
using ClrVpin.Controls;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared.Game;
using ClrVpin.Shared.FeatureType;
using PropertyChanged;

namespace ClrVpin.Shared;

[AddINotifyPropertyChangedInterface]
public class GameFiltersViewModel
{
    public GameFiltersViewModel(ListCollectionView<GameItem> gameItemsView, IGameCollections gameCollections, CommonFilterSettings commonFilterSettings, Action filterChanged)
    {
        _gameItemsView = gameItemsView;
        _commonFilterSettings = commonFilterSettings;
        _gameCollections = gameCollections;
        _filterChanged = filterChanged;

        // common views
        PresetDateOptionsView = FeatureOptions.CreateFeatureOptionsMultiSelectionView(StaticSettings.PresetDateOptions, () => new ObservableCollection<PresetDateOptionEnum>(), PresetDateSelected, null, false);
        TableManufacturedOptionsView = FeatureOptions.CreateFeatureOptionsMultiSelectionView(StaticSettings.TableManufacturedOptions, () => commonFilterSettings.SelectedManufacturedOptions,
            _ => _filterChanged(), includeSelectAll: false, minimumNumberOfSelections: 0);

        UpdateFilterViews();
    }

    // common views
    public ListCollectionView<string> TablesFilterView { get; private set; }
    public ListCollectionView<string> ManufacturersFilterView { get; private set; }
    public ListCollectionView<string> YearsBeginFilterView { get; private set; }
    public ListCollectionView<string> YearsEndFilterView { get; private set; }
    public ListCollectionView<string> TypesFilterView { get; private set; }
    public ListCollectionView<string> ApplicationFormatsFilterView { get; private set; }
    public ListCollectionView<FeatureType.FeatureType> PresetDateOptionsView { get; }
    public ListCollectionView<FeatureType.FeatureType> TableManufacturedOptionsView { get; }
    
    // feeder specific views
    public ListCollectionView<FeatureType.FeatureType> TableMatchOptionsView { get; init; }
    public ListCollectionView<FeatureType.FeatureType> UrlStatusOptionsView { get; init; }
    public ListCollectionView<FeatureType.FeatureType> OnlineFileTypeOptionsView { get; init; }
    public ListCollectionView<FeatureType.FeatureType> MiscFeaturesOptionsView { get; init; }
    
    // explorer specific views
    public ListCollectionView<FeatureType.FeatureType> MissingFilesOptionsView { get; set; }
    public ListCollectionView<FeatureType.FeatureType> TableStaleOptionsView { get; set; }
    public ListCollectionView<FeatureType.FeatureType> RomOptionsView { get; set; }
    public ListCollectionView<FeatureType.FeatureType> PupOptionsView { get; set; }

    public void Refresh(int? debounceMilliseconds = null)
    {
        TablesFilterView.RefreshDebounce(debounceMilliseconds);
        ManufacturersFilterView.RefreshDebounce(debounceMilliseconds);
        YearsBeginFilterView.RefreshDebounce(debounceMilliseconds);
        YearsEndFilterView.RefreshDebounce(debounceMilliseconds);
        TypesFilterView.RefreshDebounce(debounceMilliseconds);
        ApplicationFormatsFilterView?.RefreshDebounce(debounceMilliseconds);   // not used by all VMs, e.g. Explorer
    }

    // todo; improve: private method and only invoke once during initialization (i.e. don't recreate LCVs) using an ObservableCollection (from GameCollection) instead of List
    // - not done (yet) as this is invoked infrequently.. only via GameCollection.Update() which is invoked from DatabaseManagement.Update() when a DB entry is updated.. i.e. not a performance concern
    public void UpdateFilterViews()
    {
        // filters views (drop down combo boxes) - uses the online AND unmatched local DB 
        TablesFilterView = new ListCollectionView<string>(_gameCollections.TableNames)
        {
            // filter the table names list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
            Filter = tableName => Filter(() => _gameItemsView.Any(x => x.Name == tableName))
        };

        ManufacturersFilterView = new ListCollectionView<string>(_gameCollections.Manufacturers)
        {
            // filter the manufacturers list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
            Filter = manufacturer => Filter(() => _gameItemsView.Any(x => x.Manufacturer == manufacturer))
        };

        YearsBeginFilterView = new ListCollectionView<string>(_gameCollections.Years)
        {
            // filter the 'years from' list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
            Filter = yearString => Filter(() => _gameItemsView.Any(x => x.Year == yearString))
        };
        YearsEndFilterView = new ListCollectionView<string>(_gameCollections.Years)
        {
            // filter the 'years to' list to reflect what's displayed in the games list, i.e. taking into account ALL of the existing filter criteria
            Filter = yearString => Filter(() => _gameItemsView.Any(x => x.Year == yearString))
        };

        // table HW type, i.e. SS, EM, PM
        TypesFilterView = new ListCollectionView<string>(_gameCollections.Types)
        {
            Filter = type => Filter(() => _gameItemsView.Any(x => x.Type == type))
        };

        // table formats - vpx, fp, etc
        // - only available via online
        ApplicationFormatsFilterView = new ListCollectionView<string>(_gameCollections.Formats)
        {
            Filter = format => Filter(() => _gameItemsView.Any(x => x.OnlineGame?.TableFormats.Contains(format) == true))
        };
    }

    private bool Filter(Func<bool> dynamicFilteringFunc)
    {
        // only evaluate the func if dynamic filtering is enabled
        return !_commonFilterSettings.IsDynamicFiltering || dynamicFilteringFunc();
    }

    private void PresetDateSelected(FeatureType.FeatureType featureType)
    {
        // assign the updated at from begin date
        var offset = (PresetDateOptionEnum) featureType.Id switch
        {
            PresetDateOptionEnum.Today => (0, 0),
            PresetDateOptionEnum.Yesterday => (1, 0),
            PresetDateOptionEnum.LastThreeDays => (3, 0),
            PresetDateOptionEnum.LastFiveDays => (5, 0),
            PresetDateOptionEnum.LastWeek => (7, 0),
            PresetDateOptionEnum.LastTwoWeeks => (14, 0),
            PresetDateOptionEnum.LastMonth => (0, 1),
            PresetDateOptionEnum.LastThreeMonths => (0, 3),
            PresetDateOptionEnum.LastSixMonths => (0, 6),
            PresetDateOptionEnum.LastYear => (0, 12),
            _ => (0, 0)
        };
        UpdatedAtDateBegin(DateTime.Today.AddDays(-offset.Item1).AddMonths(-offset.Item2));

        _filterChanged();
    }

    private void UpdatedAtDateBegin(DateTime startDate)
    {
        _commonFilterSettings.SelectedUpdatedAtDateBegin = startDate;
        _commonFilterSettings.SelectedUpdatedAtDateEnd = DateTime.Today;
    }

    private readonly Action _filterChanged;
    private readonly IGameCollections _gameCollections;
    private readonly CommonFilterSettings _commonFilterSettings;
    private readonly ListCollectionView<GameItem> _gameItemsView;
}