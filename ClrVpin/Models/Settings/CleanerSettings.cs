using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ClrVpin.Models.Cleaner;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Enums;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
[Serializable]
public class CleanerSettings
{
    // empty ctor required for deserialization
    public CleanerSettings()
    {
    }

    public CleanerSettings(IEnumerable<ContentType> getFixableContentTypes)
    {
        // very important NOT to include the database type, since doing so would cause the database file(s) to be deleted
        // - deleted because would be designated as unmatched file since no table will match 'Visual Pinball'
        SelectedCheckContentTypes.AddRange(getFixableContentTypes.Select(x => x.Description).ToList());
        SelectedCheckHitTypes.AddRange(StaticSettings.AllHitTypes.Select(x => x.Enum).ToList());
        SelectedFixHitTypes.AddRange(StaticSettings.AllHitTypes.Select(x => x.Enum).ToList());
    }

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