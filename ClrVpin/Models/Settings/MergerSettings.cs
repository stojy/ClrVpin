using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ClrVpin.Models.Merger;
using ClrVpin.Models.Shared.Enums;
using PropertyChanged;
using Utils;
using Utils.Extensions;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
[Serializable]
public class MergerSettings
{
    public MergerSettings()
    {
        // default settings
        SourceFolder = SpecialFolder.Downloads;
            
        // valid hit type (i.e. a perfect name match) is always supported when rebuilding
        SelectedMatchTypes.Add(HitTypeEnum.CorrectName);
        SelectedMatchTypes.AddRange(StaticSettings.MatchTypes.Select(x => x.Enum).ToList());
        
        SelectedIgnoreCriteria.AddRange(StaticSettings.IgnoreCriteria.Select(x => x.Enum).ToList());
        SelectedMergeOptions.AddRange(StaticSettings.MergeOptions.Select(x => x.Enum).ToList());
        DeleteIgnoredFiles = true;
    }

    public string SourceFolder { get; set; }
    public string DestinationContentType { get; set; }
        
    public decimal IgnoreIfSmallerPercentage { get; set; } = 80;
    public List<string> IgnoreIWords { get; set; } = new() {"nude", "adult", "tba", "original", "orginal", "b&w", " bw ", "2scr", "2 screen", "vr room", "pcv"};

    public bool DeleteIgnoredFiles { get; set; }

    public ObservableCollection<HitTypeEnum> SelectedMatchTypes { get; init; } = new();
    public ObservableCollection<IgnoreCriteriaEnum> SelectedIgnoreCriteria { get; set; } = new();
    public ObservableCollection<MergeOptionEnum> SelectedMergeOptions { get; init; } = new();
}