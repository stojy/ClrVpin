using System.ComponentModel;

namespace ClrVpin.Models
{
    public enum HitTypeEnum
    {
        // not displayed
        [Description("Perfect Match!!")] Valid,

        [Description("Table Name")] TableName,

        [Description("Fuzzy Name")] Fuzzy,

        [Description("Wrong Case")] WrongCase,

        [Description("Duplicate")] DuplicateExtension,

        [Description("Missing")] Missing,
        
        [Description("Unknown")] Unknown    // unknown files do not relate to any specific game
    }
}