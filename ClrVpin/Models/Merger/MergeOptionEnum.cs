using System.ComponentModel;

namespace ClrVpin.Models.Merger
{
    public enum MergeOptionEnum
    {
        [Description("Preserve Date Modified Timestamp")] PreserveDateModified,
        [Description("Remove Matched Source Files")] RemoveSource
    }
}