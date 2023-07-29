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
    
    public ObservableCollection<TableMatchOptionEnum> SelectedTableMatchOptions { get; set; } = new() { TableMatchOptionEnum.LocalAndOnline, TableMatchOptionEnum.OnlineOnly, TableMatchOptionEnum.LocalOnly};
    public ObservableCollection<TableAvailabilityOptionEnum> SelectedTableDownloadOptions { get; set; } = new() { TableAvailabilityOptionEnum.Available, TableAvailabilityOptionEnum.Unavailable };

    public ObservableCollection<TableNewContentOptionEnum> SelectedTableNewContentOptions { get; set; } = new() { TableNewContentOptionEnum.TableBackglassDmd, TableNewContentOptionEnum.Other};
    public ObservableCollection<IgnoreFeatureOptionEnum> SelectedIgnoreFeatureOptions { get; set; } = new ();
}