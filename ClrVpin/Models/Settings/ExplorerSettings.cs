using System.Collections.ObjectModel;
using ClrVpin.Models.Shared;

namespace ClrVpin.Models.Settings;

public class ExplorerSettings : CommonFilterSettings
{
    public double? SelectedMinRating { get; set; }
    public double? SelectedMaxRating { get; set; }

    public ObservableCollection<ContentTypeEnum> SelectedMissingFileOptions { get; set; } = new();
    public ContentTypeEnum SelectedTableStaleOptions { get; set; } = ContentTypeEnum.TableVideos;
}

