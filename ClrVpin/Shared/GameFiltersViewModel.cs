using ClrVpin.Controls;
using ClrVpin.Models.Shared;

namespace ClrVpin.Shared;

public class GameFiltersViewModel
{
    public ListCollectionView<string> TablesFilterView { get; set; } = new();
    public ListCollectionView<string> ManufacturersFilterView { get; set; } = new();
    public ListCollectionView<string> YearsBeginFilterView { get; set; } = new();
    public ListCollectionView<string> YearsEndFilterView { get; set; } = new();
    public ListCollectionView<string> TypesFilterView { get; set; } = new();
    public ListCollectionView<string> FormatsFilterView { get; set; } = new();

    public ListCollectionView<FeatureType> TableStyleOptionsView { get; set; } = new();
    public ListCollectionView<FeatureType> TableMatchOptionsView { get; set;} = new();
    public ListCollectionView<FeatureType> TableAvailabilityOptionsView { get; set;} = new();
    public ListCollectionView<FeatureType> TableNewContentOptionsView { get; set;} = new();
    public ListCollectionView<FeatureType> PresetDateOptionsView { get; set;} = new();

    public void Refresh()
    {
        TablesFilterView.RefreshDebounce();
        ManufacturersFilterView.RefreshDebounce();
        YearsBeginFilterView.RefreshDebounce();
        YearsEndFilterView.RefreshDebounce();
        TypesFilterView.RefreshDebounce();
        FormatsFilterView.RefreshDebounce();
    }
}