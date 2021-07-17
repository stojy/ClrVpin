using System.ComponentModel;

namespace ClrVpin.Models.Rebuilder
{
    public enum MergeOptionEnum
    {
        [Description("Preserve Source Timestamp")] PreserveTimestamp,
        [Description("Remove Matched Source Files")] RemoveSource
    }
}