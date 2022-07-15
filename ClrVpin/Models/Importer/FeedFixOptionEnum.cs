using System.ComponentModel;

namespace ClrVpin.Models.Importer
{
    public enum FeedFixOptionEnum
    {
        [Description("Matched")] Matched,
        [Description("Unmatched")] Unmatched,
        [Description("Both")] Both
    }
}