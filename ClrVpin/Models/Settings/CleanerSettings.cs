using System;
using System.Collections.ObjectModel;
using System.Linq;
using ClrVpin.Models.Cleaner;
using ClrVpin.Models.Shared;
using PropertyChanged;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
[Serializable]
public class CleanerSettings
{
    public ObservableCollection<string> SelectedCheckContentTypes { get; set; } = new();
    public ObservableCollection<HitTypeEnum> SelectedCheckHitTypes { get; set; } = new();
    public ObservableCollection<HitTypeEnum> SelectedFixHitTypes { get; set; } = new();
        
    public MultipleMatchOptionEnum SelectedMultipleMatchOption { get; set; } = MultipleMatchOptionEnum.PreferMostRecentAndExceedSizeThreshold;
    public decimal MultipleMatchExceedSizeThresholdPercentage { get; set; } = 80;

    // it shouldn't be possible to select the database file since it's not selectable from the UI
    // - but with an abundance caution we explicitly ignore it since if it were included the cleaner would attempt to delete the file as 'unmatched'
    // - refer ctor
    public ContentType[] GetSelectedCheckContentTypes() => Model.Settings.GetFixableContentTypes().Where(type => SelectedCheckContentTypes.Contains(type.Description)).ToArray();
}