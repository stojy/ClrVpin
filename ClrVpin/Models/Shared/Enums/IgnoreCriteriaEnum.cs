using System.ComponentModel;

namespace ClrVpin.Models.Shared.Enums
{
    public enum IgnoreCriteriaEnum
    {
        [Description("Ignore If Not Newer")] IgnoreIfNotNewer,
        [Description("Ignore If Smaller By Percentage")] IgnoreIfSmaller,
        [Description("Ignore If Contains Words")] IgnoreIfContainsWords,
    }
}