using System.ComponentModel;

namespace ClrVpin.Models.Feeder;

public enum SimulatorOptionEnum
{
    [Description("Virtual Pinball")] VirtualPinballX,
    [Description("Future Pinball")] FuturePinball,    
    [Description("FX Pinball")] PinballFx,
    [Description("Unknown")] Unknown
}