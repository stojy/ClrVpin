using ClrVpin.Models.Shared;

namespace ClrVpin.Models.Settings;

public class ExplorerSettings : CommonFilterSettings
{
    public double? SelectedMinRating { get; set; }
    public double? SelectedMaxRating { get; set; }

    // todo; checkbox with list
    public ContentTypeEnum SelectedTableMissingOptions { get; set; } = ContentTypeEnum.Tables;
    public ContentTypeEnum SelectedTableStaleOptions { get; set; } = ContentTypeEnum.TableVideos;
}

