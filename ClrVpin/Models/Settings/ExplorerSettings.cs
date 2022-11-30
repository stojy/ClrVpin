using System;
using PropertyChanged;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
public class ExplorerSettings
{
    public bool IsDynamicFiltering { get; set; }

    public string SelectedTableFilter { get; set; }
    public string SelectedManufacturerFilter { get; set; }

    public string SelectedTypeFilter { get; set; }
    
    public string SelectedYearBeginFilter { get; set; }
    public string SelectedYearEndFilter { get; set; }

    public DateTime? SelectedUpdatedAtDateBegin { get; set; }
    public DateTime? SelectedUpdatedAtDateEnd { get; set; }
}