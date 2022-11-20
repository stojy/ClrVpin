using System.ComponentModel;

namespace ClrVpin.Models.Feeder;

public enum PresetDateOptionEnum
{
    [Description("Today")] Today,
    [Description("Yesterday")] Yesterday,
    [Description("Last 3 days")] LastThreeDays,
    [Description("Last week")] LastWeek,
    [Description("Last month")] LastMonth,
    [Description("Last 3 months")] LastThreeMonths,
    [Description("Last year")] LastYear,
}