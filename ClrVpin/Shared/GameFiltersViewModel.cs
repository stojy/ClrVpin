using System;
using System.Collections.Generic;
using System.Linq;
using ClrVpin.Controls;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Settings;
using ClrVpin.Models.Shared;
using Utils;

namespace ClrVpin.Shared;

public class GameFiltersViewModel
{
    public GameFiltersViewModel(Action filterChanged, Action<DateTime?> updatedAtDateBegin)
    {
        _updatedAtDateBegin = updatedAtDateBegin;
        _filterChanged = filterChanged;

        PresetDateOptionsView = CreatePresetDateOptionsView(StaticSettings.PresetDateOptions);
    }

    public ListCollectionView<string> TablesFilterView { get; set; } = new();
    public ListCollectionView<string> ManufacturersFilterView { get; set; } = new();
    public ListCollectionView<string> YearsBeginFilterView { get; set; } = new();
    public ListCollectionView<string> YearsEndFilterView { get; set; } = new();
    public ListCollectionView<string> TypesFilterView { get; set; } = new();
    public ListCollectionView<string> FormatsFilterView { get; set; } = new();

    public ListCollectionView<FeatureType> TableStyleOptionsView { get; set; } = new();
    public ListCollectionView<FeatureType> TableMatchOptionsView { get; set; } = new();
    public ListCollectionView<FeatureType> TableAvailabilityOptionsView { get; set; } = new();
    public ListCollectionView<FeatureType> TableNewContentOptionsView { get; set; } = new();
    public ListCollectionView<FeatureType> PresetDateOptionsView { get; }

    public void Refresh()
    {
        TablesFilterView.RefreshDebounce();
        ManufacturersFilterView.RefreshDebounce();
        YearsBeginFilterView.RefreshDebounce();
        YearsEndFilterView.RefreshDebounce();
        TypesFilterView.RefreshDebounce();
        FormatsFilterView.RefreshDebounce();
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
                        PresetDateOptionEnum.LastThreeDays => (3, 0),
                        PresetDateOptionEnum.LastWeek => (7, 0),
                        PresetDateOptionEnum.LastMonth => (0, 1),
                        PresetDateOptionEnum.LastThreeMonths => (0, 3),
                        PresetDateOptionEnum.LastYear => (0, 12),
                        _ => (0, 0)
                    };
                    _updatedAtDateBegin(DateTime.Today.AddDays(-offset.Item1).AddMonths(-offset.Item2));

                    _filterChanged();
                })
            };
            return featureType;
        }).ToList();

        return new ListCollectionView<FeatureType>(featureTypes);
    }

    private readonly Action _filterChanged;
    private readonly Action<DateTime?> _updatedAtDateBegin;
}