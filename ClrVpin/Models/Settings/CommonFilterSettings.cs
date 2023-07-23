using System;
using System.Collections.ObjectModel;
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

    // todo; replace with collection?
    public TableStyleOptionEnum SelectedTableStyleOption { get; set; } = TableStyleOptionEnum.Manufactured;
    public ObservableCollection<string> SelectedTableStyleOptions { get; set; } = new() { TableStyleOptionEnum.Manufactured.ToString(), TableStyleOptionEnum.Original.ToString() };
}