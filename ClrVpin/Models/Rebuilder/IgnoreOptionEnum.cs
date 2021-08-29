using System.ComponentModel;

namespace ClrVpin.Models.Rebuilder
{
    public enum IgnoreOptionEnum
    {
        [Description("Ignore If Not Newer")] IgnoreIfNotNewer,
        [Description("Ignore If Smaller By Percentage")] IgnoreIfSmaller,
    }
}