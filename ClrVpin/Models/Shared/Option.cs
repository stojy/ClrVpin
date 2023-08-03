using PropertyChanged;

namespace ClrVpin.Models.Shared;

[AddINotifyPropertyChangedInterface]
public class Option
{
    public string Description { get; set; }
    public string Tip { get; init; }
    
    public bool IsHighlighted { get; protected init; }
 
    public string HelpUrl { get; protected init; }
}