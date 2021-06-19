using System.ComponentModel;

namespace ClrVpin.Models
{
    public enum MergeOptionEnum
    {
        [Description("Preserve Source Timestamp")] PreserveTimestamp,
        [Description("Ignore Older Files")] IgnoreOlder,
        [Description("Ignore Smaller Files (90% or less)")] IgnoreSmaller,
        [Description("Removed Matched Source Files")] RemoveSource
    }
}