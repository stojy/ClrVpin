using PropertyChanged;

namespace ClrVpin.Models.Shared;

[AddINotifyPropertyChangedInterface]
public class Option
{
    public string Description { get; set; }
    public string Tip { get; init; }
}