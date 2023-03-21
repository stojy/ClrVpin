using System.Collections.ObjectModel;
using ClrVpin.Models.Feeder;
using ClrVpin.Models.Shared;

namespace ClrVpin.Models.Settings;

public class FeederSettings : CommonFilterSettings
{
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - setter required for json.net
    public ObservableCollection<HitTypeEnum> SelectedMatchCriteriaOptions { get; set; } = new();

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - setter required for json.net
    public ObservableCollection<FixFeedOptionEnum> SelectedFeedFixOptions { get; set; } = new();

    // display result filtering criteria
    
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global - setter is assigned member expression, refer Accessor.cs
    public TableMatchOptionEnum SelectedTableMatchOption { get; set; } = TableMatchOptionEnum.All;
    public TableAvailabilityOptionEnum SelectedTableAvailabilityOption { get; set; } = TableAvailabilityOptionEnum.Any;
    public TableNewContentOptionEnum SelectedTableNewContentOption { get; set; } = TableNewContentOptionEnum.Any;
    public ObservableCollection<IgnoreFeatureOptionEnum> SelectedIgnoreFeatureOptions { get; set; } = new ();
    // ReSharper restore AutoPropertyCanBeMadeGetOnly.Global
}