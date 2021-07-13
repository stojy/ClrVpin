using System.ComponentModel;

namespace ClrVpin.Models
{
    public enum HitTypeEnum
    {
        // not displayed
        [Description("Perfect Match")] Valid,

        [Description("Table Name Match")] TableName,

        [Description("Fuzzy Name Match")] Fuzzy,

        [Description("Wrong Case")] WrongCase,

        [Description("Duplicate Extension")] DuplicateExtension,

        [Description("Missing File")] Missing,
        
        [Description("Unknown Table File")] Unknown,    // supported file extension, but unable to be matched against a game/table
        
        [Description("Unsupported File Type")] Unsupported    // unsupported file type (extension).. and thus also can't be matched against a game/table
    }
}