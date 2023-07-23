using System.ComponentModel;

namespace ClrVpin.Models.Feeder
{
    public enum TableStyleOptionEnum
    {
        [Description("Manufactured")] Manufactured,
        [Description("Original")] Original,
        // todo; remove this after SelectedTableStyleOptions is exclusively used
        [Description("Both")] Both
    }
}