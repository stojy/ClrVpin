using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Shared.Enums;
using Utils.Extensions;

namespace ClrVpin.Models.Settings;

[Serializable]
public class FeederSettings : CommonFilterSettings
{
    public FeederSettings()
    {
        // default settings
        SelectedMatchCriteriaOptions.Add(HitTypeEnum.Fuzzy);
        SelectedFeedFixOptions.AddRange(StaticSettings.FixFeedOptions.Select(x => x.Enum).ToList());
        SelectedTableMatchOptions.AddRange(StaticSettings.TableMatchOptions.Select(x => x.Enum).ToList());
        SelectedTableDownloadOptions.AddRange(StaticSettings.TableDownloadOptions.Select(x => x.Enum).ToList());
        SelectedTableNewFileOptions.AddRange(new List<string>
        {
            TableNewFileOptionEnum.Tables.GetDescription(),
            TableNewFileOptionEnum.Backglasses.GetDescription(),
            TableNewFileOptionEnum.DMDs.GetDescription(),
        });
    }

    public ObservableCollection<HitTypeEnum> SelectedMatchCriteriaOptions { get; set; } = new();
    public ObservableCollection<FixFeedOptionEnum> SelectedFeedFixOptions { get; set; } = new();

    public ObservableCollection<TableMatchOptionEnum> SelectedTableMatchOptions { get; set; } = new();
    public ObservableCollection<TableDownloadOptionEnum> SelectedTableDownloadOptions { get; set; } = new();

    public ObservableCollection<string> SelectedTableNewFileOptions { get; set; } = new();
    public ObservableCollection<IgnoreFeatureOptionEnum> SelectedIgnoreFeatureOptions { get; set; } = new ();
}