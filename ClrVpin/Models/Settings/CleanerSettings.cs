using System.Collections.ObjectModel;
using ClrVpin.Models.Cleaner;
using ClrVpin.Models.Shared;
using PropertyChanged;

namespace ClrVpin.Models.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class CleanerSettings
    {
        public ObservableCollection<string> SelectedCheckContentTypes { get; set; } = new();
        public ObservableCollection<HitTypeEnum> SelectedCheckHitTypes { get; set; } = new();
        public ObservableCollection<HitTypeEnum> SelectedFixHitTypes { get; set; } = new();
        
        public MultipleMatchOptionEnum SelectedMultipleMatchOption { get; set; } = MultipleMatchOptionEnum.PreferMostRecentAndExceedSizeThreshold;
        public decimal MultipleMatchExceedSizeThresholdPercentage { get; set; } = 85;
    }
}