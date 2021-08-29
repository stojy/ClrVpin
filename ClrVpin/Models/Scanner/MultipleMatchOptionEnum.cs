using System.ComponentModel;

namespace ClrVpin.Models.Scanner
{
    public enum MultipleMatchOptionEnum
    {
        [Description("Prefer Correct Name")] PreferCorrectName,
        [Description("Prefer Most Recent")] PreferMostRecent,
        [Description("Prefer Largest Size")] PreferLargestSize
    }
}