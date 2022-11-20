using System.ComponentModel;

namespace ClrVpin.Models.Feeder
{
    public enum TableMatchOptionEnum
    {
        [Description("Matched")] LocalAndOnline, // local and online
        [Description("Unmatched")] LocalOnly,    // only in local
        [Description("Missing")] OnlineOnly,     // only in online
        [Description("All")] All
    }
}