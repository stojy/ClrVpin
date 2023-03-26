using System;
using ClrVpin.Models.Feeder;
using PropertyChanged;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
[Serializable]
public class CommonFilterSettings
{
    public bool IsDynamicFiltering { get; set; }

    public string SelectedTableFilter { get; set; }
    public string SelectedManufacturerFilter { get; set; }

    public string SelectedTypeFilter { get; set; }
    
    public string SelectedYearBeginFilter { get; set; }
    public string SelectedYearEndFilter { get; set; }

    public string SelectedFormatFilter { get; set; }

    public DateTime? SelectedUpdatedAtDateBegin { get; set; }
    public DateTime? SelectedUpdatedAtDateEnd { get; set; }

    public TableStyleOptionEnum SelectedTableStyleOption { get; set; } = TableStyleOptionEnum.Manufactured;
}