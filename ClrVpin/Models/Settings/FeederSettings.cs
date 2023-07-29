using System;
using System.Collections.ObjectModel;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Shared;

namespace ClrVpin.Models.Settings;

[Serializable]
public class FeederSettings : CommonFilterSettings
{
    public ObservableCollection<HitTypeEnum> SelectedMatchCriteriaOptions { get; set; } = new();

    public ObservableCollection<FixFeedOptionEnum> SelectedFeedFixOptions { get; set; } = new();

    // display result filtering criteria
    
    public TableMatchOptionEnum SelectedTableMatchOption { get; set; } = TableMatchOptionEnum.All;
    public ObservableCollection<TableAvailabilityOptionEnum> SelectedTableDownloadOptions { get; set; } = new() { TableAvailabilityOptionEnum.Available, TableAvailabilityOptionEnum.Unavailable };

    public TableNewContentOptionEnum SelectedTableNewContentOption { get; set; } = TableNewContentOptionEnum.Any;
    public ObservableCollection<IgnoreFeatureOptionEnum> SelectedIgnoreFeatureOptions { get; set; } = new ();
}