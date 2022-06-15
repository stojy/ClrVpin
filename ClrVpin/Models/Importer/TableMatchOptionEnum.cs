using System.ComponentModel;

namespace ClrVpin.Models.Importer
{
    public enum TableMatchOptionEnum
    {
        [Description("Matched")] Matched,
        [Description("Unmatched")] Unmatched,
        [Description("Both")] Both
    }
}