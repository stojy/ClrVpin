using System.ComponentModel;

namespace ClrVpin.Models.Importer
{
    public enum TableMatchOptionEnum
    {
        [Description("Matched")] LocalAndOnline, // local and online
        [Description("Unmatched")] LocalOnly,    // only in local
        [Description("Missing")] OnlineOnly,     // only in online
        [Description("All")] All
    }
}