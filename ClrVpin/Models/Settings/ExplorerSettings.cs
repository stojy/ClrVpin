using System;
using ClrVpin.Models.Feeder;

namespace ClrVpin.Models.Settings;

public class ExplorerSettings : CommonSettings
{
    public string SelectedTableFilter { get; set; }
    public string SelectedManufacturerFilter { get; set; }

    public string SelectedTypeFilter { get; set; }
    
    public string SelectedYearBeginFilter { get; set; }
    public string SelectedYearEndFilter { get; set; }

    public DateTime? SelectedUpdatedAtDateBegin { get; set; }
    public DateTime? SelectedUpdatedAtDateEnd { get; set; }
    
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - setter is assigned member expression, refer Accessor.cs
    public TableStyleOptionEnum SelectedTableStyleOption { get; set; } = TableStyleOptionEnum.Manufactured;

    public double? SelectedMinRating { get; set; }
    public double? SelectedMaxRating { get; set; }
}

