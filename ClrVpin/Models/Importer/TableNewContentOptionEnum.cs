using System.ComponentModel;

namespace ClrVpin.Models.Importer;

public enum TableNewContentOptionEnum
{
    [Description("Table, Backglass, DMD")] TableBackglassDmd,
    [Description("Other")] Other,
    [Description("Any")] Any
}