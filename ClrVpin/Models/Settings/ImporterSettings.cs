using System;
using System.Collections.ObjectModel;
using ClrVpin.Models.Shared;
using PropertyChanged;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
public class ImporterSettings
{
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - setter required for json.net
    public ObservableCollection<HitTypeEnum> SelectedMatchTypes { get; set; } = new ObservableCollection<HitTypeEnum>();

    // display result filtering criteria
    public bool IncludeOriginalTables { get; set; }
    public bool IncludeUnmatchedTables { get; set; } = true;
    public DateTime? UpdatedAtDateBegin { get; set; }
    public DateTime? UpdatedAtDateEnd { get; set; }
}