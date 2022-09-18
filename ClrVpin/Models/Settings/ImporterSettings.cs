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
    public ObservableCollection<HitTypeEnum> SelectedMatchCriteriaOptions { get; set; } = new();

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - setter required for json.net
    public ObservableCollection<FixFeedOptionEnum> SelectedFeedFixOptions { get; set; } = new();

    // display result filtering criteria
    public TableStyleOptionEnum SelectedTableStyleOption { get; set; } = TableStyleOptionEnum.Manufactured;
    public TableMatchOptionEnum SelectedTableMatchOption { get; set; } = TableMatchOptionEnum.LocalAndOnline;
    public TableAvailabilityOptionEnum SelectedTableAvailabilityOption { get; set; } = TableAvailabilityOptionEnum.Both;
    public TableNewContentOptionEnum SelectedTableNewContentOption { get; set; } = TableNewContentOptionEnum.TableBackglassDmd;
    public DateTime? UpdatedAtDateBegin { get; set; }
    public DateTime? UpdatedAtDateEnd { get; set; }
}