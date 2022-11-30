using ClrVpin.Controls;

namespace ClrVpin.Shared;

public class GameFiltersViewModel
{
    public ListCollectionView<string> TablesFilterView { get; set; }
    public ListCollectionView<string> ManufacturersFilterView { get; set; }
    public ListCollectionView<string> YearsBeginFilterView { get; set; }
    public ListCollectionView<string> YearsEndFilterView { get; set; }
    public ListCollectionView<string> TypesFilterView { get; set; }
    public ListCollectionView<string> FormatsFilterView { get; set; }

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