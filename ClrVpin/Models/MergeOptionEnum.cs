using System.ComponentModel;

namespace ClrVpin.Models
{
    public enum MergeOptionEnum
    {
        [Description("Preserve Source Timestamp")] PreserveTimestamp,
        [Description("Ignore Older Files")] IgnoreOlder,
        [Description("Ignore Smaller Files (less than 50%)")] IgnoreSmaller,
        [Description("Remove Matched Source Files")] RemoveSource
    }
}