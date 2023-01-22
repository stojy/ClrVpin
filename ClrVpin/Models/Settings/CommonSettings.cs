using PropertyChanged;

namespace ClrVpin.Models.Settings;

[AddINotifyPropertyChangedInterface]
public class CommonSettings
{
    public bool IsDynamicFiltering { get; set; }
}