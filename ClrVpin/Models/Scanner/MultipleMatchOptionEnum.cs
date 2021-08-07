using System.ComponentModel;

namespace ClrVpin.Models.Scanner
{
    public enum MultipleMatchOptionEnum
    {
        [Description("Correct Name")] CorrectName,
        [Description("Most Recent")] MostRecent,
        [Description("Largest Size")] LargestSize
    }
}