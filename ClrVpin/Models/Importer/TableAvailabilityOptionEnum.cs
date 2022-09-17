using System.ComponentModel;

namespace ClrVpin.Models.Importer
{
    public enum TableAvailabilityOptionEnum
    {
        [Description("Available")] Available,
        [Description("Unavailable")] Unavailable,
        [Description("Both")] Both
    }
}