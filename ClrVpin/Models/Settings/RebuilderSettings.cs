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
            
            // valid hit type (i.e. a perfect name match) is always supported when rebuilding
            SelectedMatchTypes.Add(HitTypeEnum.Valid);
        }

        public string SourceFolder { get; set; }
        public string DestinationContentType { get; set; }

        public ObservableCollection<HitTypeEnum> SelectedMatchTypes { get; set; } = new ObservableCollection<HitTypeEnum>();
        public ObservableCollection<MergeOptionEnum> SelectedMergeOptions { get; set; } = new ObservableCollection<MergeOptionEnum>();
        public ObservableCollection<IgnoreOptionEnum> SelectedIgnoreOptions { get; set; } = new ObservableCollection<IgnoreOptionEnum>();
    }
}