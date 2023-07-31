using System;
using System.Collections.ObjectModel;
using ClrVpin.Models.Shared;

namespace ClrVpin.Models.Settings;

[Serializable]
public class ExplorerSettings : CommonFilterSettings
{
    public double? SelectedMinRating { get; set; }
    public double? SelectedMaxRating { get; set; }

    public ObservableCollection<ContentTypeEnum> SelectedMissingFileOptions { get; set; } = new();
    public ObservableCollection<ContentTypeEnum> SelectedTableStaleOptions { get; set; } = new ();
    public ObservableCollection<YesNoNullableBooleanOptionEnum> SelectedTableRomOptions { get; set; } = new ();
}
