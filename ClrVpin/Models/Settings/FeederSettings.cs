using System;
using System.Collections.ObjectModel;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Shared.Enums;

namespace ClrVpin.Models.Settings;

[Serializable]
public class FeederSettings : CommonFilterSettings
{
    // defaults assigned via Settings

    public ObservableCollection<HitTypeEnum> SelectedMatchCriteriaOptions { get; set; } = new();
    public ObservableCollection<FixFeedOptionEnum> SelectedFeedFixOptions { get; set; } = new();

    public ObservableCollection<TableMatchOptionEnum> SelectedTableMatchOptions { get; set; } = new();
    public ObservableCollection<TableDownloadOptionEnum> SelectedTableDownloadOptions { get; set; } = new();

    public ObservableCollection<string> SelectedTableNewFileOptions { get; set; } = new();
    public ObservableCollection<IgnoreFeatureOptionEnum> SelectedIgnoreFeatureOptions { get; set; } = new ();
}