using System.ComponentModel;

namespace ClrVpin.Models.Shared.Enums;

public enum TechnologyTypeOptionEnum
{
    [Description("Solid State")] SS,
    [Description("Electro Mechanical")] EM,    
    [Description("Pure Mechanical")] PM,
    [Description("Unknown")] Unknown
}