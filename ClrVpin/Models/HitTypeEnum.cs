using System.ComponentModel;

namespace ClrVpin.Models
{
    public enum HitTypeEnum
    {
        // not displayed
        [Description("Valid")] Valid,

        [Description("Table Name Match")] TableName,

        [Description("Fuzzy Name Match")] Fuzzy,

        [Description("Wrong Case")] WrongCase,

        [Description("Duplicate Extension")] DuplicateExtension,

        [Description("Missing")] Missing,
        
        [Description("Unknown")] Unknown    // unknown files do not relate to any specific game
    }
}