using System.ComponentModel;

namespace ClrVpin.Models.Rebuilder
{
    public enum MergeOptionEnum
    {
        [Description("Preserve Date Modified Timestamp")] PreserveDateModified,
        [Description("Remove Matched Source Files")] RemoveSource
    }
}