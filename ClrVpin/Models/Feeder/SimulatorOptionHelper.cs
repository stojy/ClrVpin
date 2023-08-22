namespace ClrVpin.Models.Feeder;

public static class SimulatorOptionHelper
{
    public static SimulatorOptionEnum? GetEnum(string stringType)
    {
        SimulatorOptionEnum? simulatorOptionEnum = stringType switch
        {
            SimulatorAbbreviationEnum.VirtualPinballX => SimulatorOptionEnum.VirtualPinballX,
            SimulatorAbbreviationEnum.FuturePinball => SimulatorOptionEnum.FuturePinball,
            SimulatorAbbreviationEnum.PinballFx => SimulatorOptionEnum.PinballFx,
            SimulatorAbbreviationEnum.PinballFx2 => SimulatorOptionEnum.PinballFx,
            SimulatorAbbreviationEnum.PinballFx3 => SimulatorOptionEnum.PinballFx,
            _ => null
        };

        return simulatorOptionEnum;
    }
}