using System.ComponentModel;

namespace ClrVpin.Models.Feeder;

public enum PresetDateOptionEnum
{
    [Description("Today")] Today,
    [Description("Yesterday")] Yesterday,
    [Description("Last 3 days")] LastThreeDays,
    [Description("Last 5 days")] LastFiveDays,
    [Description("Last week")] LastWeek,
    [Description("Last 2 weeks")] LastTwoWeeks,
    [Description("Last month")] LastMonth,
    [Description("Last 3 months")] LastThreeMonths,
    [Description("Last 6 months")] LastSixMonths,
    [Description("Last year")] LastYear,
}