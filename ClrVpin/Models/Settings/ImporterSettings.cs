using System;
using System.Collections.ObjectModel;
using ClrVpin.Models.Importer;
using ClrVpin.Models.Shared;
using PropertyChanged;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
public class ImporterSettings
{
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - setter required for json.net
    public ObservableCollection<HitTypeEnum> SelectedMatchCriteriaOptions { get; set; } = new ObservableCollection<HitTypeEnum>();

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - setter required for json.net
    public ObservableCollection<FixFeedOptionEnum> SelectedFeedFixOptions { get; set; } = new ObservableCollection<FixFeedOptionEnum>();

    // display result filtering criteria
    public TableStyleOptionEnum SelectedTableStyleOption { get; set; } = TableStyleOptionEnum.Manufactured;
    public TableMatchOptionEnum SelectedTableMatchOption { get; set; } = TableMatchOptionEnum.LocalAndOnline;
    public DateTime? UpdatedAtDateBegin { get; set; }
    public DateTime? UpdatedAtDateEnd { get; set; }
}