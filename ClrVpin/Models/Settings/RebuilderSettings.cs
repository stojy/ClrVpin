using System.Collections.Generic;
using System.Collections.ObjectModel;
using ClrVpin.Models.Rebuilder;
using ClrVpin.Models.Shared;
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
        public List<string> IgnoreIWords { get; set; } = new List<string>{"nude", "adult", "tba", "original", "orginal", "b&w", " bw ", "2scr", "2 screen"};

        public bool DeleteIgnoredFiles { get; set; }

        public ObservableCollection<HitTypeEnum> SelectedMatchTypes { get; set; } = new ObservableCollection<HitTypeEnum>();
        public ObservableCollection<IgnoreCriteriaEnum> SelectedIgnoreCriteria { get; set; } = new ObservableCollection<IgnoreCriteriaEnum>();
        public ObservableCollection<MergeOptionEnum> SelectedMergeOptions { get; set; } = new ObservableCollection<MergeOptionEnum>();
    }
}