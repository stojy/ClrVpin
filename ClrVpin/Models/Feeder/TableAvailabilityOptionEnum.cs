using System.ComponentModel;

namespace ClrVpin.Models.Feeder
{
    public enum TableAvailabilityOptionEnum
    {
        [Description("Available")] Available,
        [Description("Unavailable")] Unavailable,
        [Description("Any")] Any
    }
}