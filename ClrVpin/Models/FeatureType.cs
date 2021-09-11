using System.Windows.Input;
using PropertyChanged;
using Utils;

namespace ClrVpin.Models
{
    [AddINotifyPropertyChangedInterface]
    public class FeatureType
    {
        public FeatureType(int id)
        {
            Id = id;
        }

        public int Id { get; set; } // unique identifier with the scope of the other feature types, e.g. HitType.Enum
        public string Description { get; set; }
        public string Tip { get; set; }
        public bool IsSupported { get; set; }
        public bool IsNeverSupported { get; set; }
        public bool IsActive { get; set; }
        public ICommand SelectedCommand { get; set; }
        public bool IsHighlighted { get; set; }
        public bool IsHelpSupported { get; set; }
        public ActionCommand HelpAction { get; set; }
    }
}