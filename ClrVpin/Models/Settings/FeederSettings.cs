using System;
using System.Collections.ObjectModel;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Shared;

namespace ClrVpin.Models.Settings;

public class FeederSettings : CommonSettings
{
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - setter required for json.net
    public ObservableCollection<HitTypeEnum> SelectedMatchCriteriaOptions { get; set; } = new();

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - setter required for json.net
    public ObservableCollection<FixFeedOptionEnum> SelectedFeedFixOptions { get; set; } = new();

    // display result filtering criteria
    
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global - setter is assigned member expression, refer Accessor.cs
    public TableStyleOptionEnum SelectedTableStyleOption { get; set; } = TableStyleOptionEnum.Manufactured;
    public TableMatchOptionEnum SelectedTableMatchOption { get; set; } = TableMatchOptionEnum.All;
    public TableAvailabilityOptionEnum SelectedTableAvailabilityOption { get; set; } = TableAvailabilityOptionEnum.Any;
    public TableNewContentOptionEnum SelectedTableNewContentOption { get; set; } = TableNewContentOptionEnum.Any;
    // ReSharper restore AutoPropertyCanBeMadeGetOnly.Global
    
    public string SelectedTableFilter { get; set; }
    public string SelectedManufacturerFilter { get; set; }

    public string SelectedTypeFilter { get; set; }
    public string SelectedFormatFilter { get; set; }
    
    public string SelectedYearBeginFilter { get; set; }
    public string SelectedYearEndFilter { get; set; }

    public DateTime? SelectedUpdatedAtDateBegin { get; set; }
    public DateTime? SelectedUpdatedAtDateEnd { get; set; }
}