using System.Collections.ObjectModel;
using ClrVpin.Models.Scanner;
using ClrVpin.Models.Shared;
using PropertyChanged;

namespace ClrVpin.Models.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class ScannerSettings
    {
        public ObservableCollection<string> SelectedCheckContentTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<HitTypeEnum> SelectedCheckHitTypes { get; set; } = new ObservableCollection<HitTypeEnum>();
        public ObservableCollection<HitTypeEnum> SelectedFixHitTypes { get; set; } = new ObservableCollection<HitTypeEnum>();
        
        public MultipleMatchOptionEnum SelectedMultipleMatchOption { get; set; } = MultipleMatchOptionEnum.PreferMostRecentAndExceedSizeThreshold;
        public decimal MultipleMatchExceedSizeThresholdPercentage { get; set; } = 85;
    }
}