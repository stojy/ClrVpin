using System.Collections.Generic;
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
            SelectedMatchTypes.Add(HitTypeEnum.CorrectName);
        }

        public string SourceFolder { get; set; }
        public string DestinationContentType { get; set; }

        public decimal IgnoreIfSmallerPercentage { get; set; } = 75;
        public List<string> IgnoreIWords { get; set; } = new List<string>{"nude", "adult", "tba", "original", "orginal", "b&w"};    // orginal - added as this appears to be a popular typo!

        public bool DeleteIgnoredFiles { get; set; }

        public ObservableCollection<HitTypeEnum> SelectedMatchTypes { get; set; } = new ObservableCollection<HitTypeEnum>();
        public ObservableCollection<MergeOptionEnum> SelectedMergeOptions { get; set; } = new ObservableCollection<MergeOptionEnum>();
        public ObservableCollection<IgnoreOptionEnum> SelectedIgnoreOptions { get; set; } = new ObservableCollection<IgnoreOptionEnum>();
    }
}