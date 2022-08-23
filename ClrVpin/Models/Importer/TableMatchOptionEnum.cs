using System.ComponentModel;

namespace ClrVpin.Models.Importer
{
    public enum TableMatchOptionEnum
    {
        [Description("Matched")] LocalAndOnline, // local and online
        [Description("Missing")] OnlineOnly,     // only in online
        [Description("Unmatched")] LocalOnly,    // only in local
        [Description("All")] All
    }
}