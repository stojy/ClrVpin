using System.ComponentModel;

namespace ClrVpin.Models.Rebuilder
{
    public enum IgnoreOptionEnum
    {
        [Description("Ignore Older Files")] IgnoreOlder,
        [Description("Ignore Smaller Files (less than 50%)")] IgnoreSmaller,
    }
}