using System.Windows.Input;
using PropertyChanged;

namespace ClrVpin.Shared.FeatureType;

[AddINotifyPropertyChangedInterface]
public class FeatureType
{
    public FeatureType(int id)
    {
        Id = id;
    }

    public int Id { get; set; } // unique identifier with the scope of the other feature types, e.g. HitType.Enum
    public string Description { get; init; }
    public string Tip { get; set; }
    public bool IsSupported { get; set; }
    public bool IsNeverSupported { get; init; }
    public bool IsActive { get; set; }
    public ICommand SelectedCommand { get; set; }
    public bool IsHighlighted { get; init; }
    public bool IsHelpSupported { get; set; }
    public ICommand HelpAction { get; init; }
    public bool IsSpecial { get; set; }
    public string Tag { get; init; } // arbitrary tagging value, e.g. to be used to identify a type uniquely for RadioButton.GroupName
}