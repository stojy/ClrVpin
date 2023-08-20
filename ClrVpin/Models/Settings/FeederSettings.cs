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
        SelectedUrlStatusOptions.AddRange(StaticSettings.UrlStatusOptions.Select(x => x.Enum).ToList());
        SelectedOnlineFileTypeOptions.AddRange(new List<string>
        {
            OnlineFileTypeEnum.Tables.GetDescription(),
            OnlineFileTypeEnum.Backglasses.GetDescription(),
            OnlineFileTypeEnum.DMDs.GetDescription(),
        });

        SelectedFormatFilter = "VPX";
    }

    public ObservableCollection<HitTypeEnum> SelectedMatchCriteriaOptions { get; set; } = new();
    public ObservableCollection<FixFeedOptionEnum> SelectedFeedFixOptions { get; set; } = new();

    public ObservableCollection<TableMatchOptionEnum> SelectedTableMatchOptions { get; set; } = new();
    public ObservableCollection<UrlStatusEnum> SelectedUrlStatusOptions { get; set; } = new();

    public ObservableCollection<string> SelectedOnlineFileTypeOptions { get; set; } = new();
    public ObservableCollection<MiscFeatureOptionEnum> SelectedMiscFeatureOptions { get; set; } = new ();

    public string SelectedFormatFilter { get; set; }
}