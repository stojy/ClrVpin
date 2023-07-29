using System.ComponentModel;

namespace ClrVpin.Models.Feeder;

public enum TableMatchOptionEnum
{
    [Description("Local and Online")] LocalAndOnline,
    [Description("Local Only")] LocalOnly,    
    [Description("Online Only")] OnlineOnly,  
}