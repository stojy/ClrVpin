using System.Windows.Input;
using PropertyChanged;

namespace ClrVpin.Models
{
    [AddINotifyPropertyChangedInterface]
    public class FeatureType
    {
        public string Description { get; set; }
        public string Tip { get; set; }
        public bool IsSupported { get; set; }
        public bool IsNeverSupported { get; set; }
        public bool IsActive { get; set; }
        public ICommand SelectedCommand { get; set; }
    }
}