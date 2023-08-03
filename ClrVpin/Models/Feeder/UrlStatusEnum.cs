using System.ComponentModel;

namespace ClrVpin.Models.Feeder;

public enum UrlStatusEnum
{
    [Description("Valid")]
    Valid,
    [Description("Broken")]
    Broken,
    [Description("Missing")]
    Missing
}