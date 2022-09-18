using System.ComponentModel;

namespace ClrVpin.Models.Importer;

public enum TableNewContentOptionEnum
{
    [Description("Table, Backglass, DMDs")] TableBackglassDmd,
    [Description("Other")] Other,
    [Description("All")] All
}