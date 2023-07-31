using System.ComponentModel;

namespace ClrVpin.Models.Shared.Enums;

public enum YesNoNullableBooleanOptionEnum
{
    [Description("Yes")] True,
    [Description("No")] False,    
    [Description("Don't Care")] DontCare,    
}