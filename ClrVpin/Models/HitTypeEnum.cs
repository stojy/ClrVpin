using System.ComponentModel;

namespace ClrVpin.Models
{
    public enum HitTypeEnum
    {
        // not displayed
        [Description("Perfect Match (no fixes required)")] Valid,

        [Description("Table Name Match")] TableName,

        [Description("Fuzzy Name Match")] Fuzzy,

        [Description("Wrong Case")] WrongCase,

        [Description("Duplicate Extension")] DuplicateExtension,

        [Description("Missing File")] Missing,
        
        [Description("Unknown Table")] Unknown,    // unknown files do not relate to any specific game
        
        [Description("Unsupported File Type")] Unsupported    // unsupported files do not relate to any supported extension
    }
}