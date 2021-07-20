using System.Collections.ObjectModel;
using ClrVpin.Models.Rebuilder;
using PropertyChanged;
using Utils;

namespace ClrVpin.Models.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class RebuilderSettings
    {
        public RebuilderSettings()
        {
            SourceFolder = SpecialFolder.Downloads;
        }

        public string SourceFolder { get; set; }
        public string DestinationContentType { get; set; }

        public ObservableCollection<HitTypeEnum> SelectedMatchTypes { get; set; } = new ObservableCollection<HitTypeEnum>();
        public ObservableCollection<MergeOptionEnum> SelectedMergeOptions { get; set; } = new ObservableCollection<MergeOptionEnum>();
        public ObservableCollection<IgnoreOptionEnum> SelectedIgnoreOptions { get; set; } = new ObservableCollection<IgnoreOptionEnum>();

    }
}