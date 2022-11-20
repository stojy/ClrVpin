using System.ComponentModel;

namespace ClrVpin.Models.Cleaner;

public enum MultipleMatchOptionEnum
{
    [Description("Prefer Most Recent")] PreferMostRecent,
    [Description("Prefer Largest Size")] PreferLargestSize,
    [Description("Prefer Most Recent and Exceeds Size Threshold")] PreferMostRecentAndExceedSizeThreshold,
    [Description("Prefer Correct Name")] PreferCorrectName
}