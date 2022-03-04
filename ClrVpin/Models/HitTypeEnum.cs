using System.ComponentModel;

namespace ClrVpin.Models
{
    // decreasing priority order.. important when using 'name' as the multiple file preference
    public enum HitTypeEnum
    {
        [Description("Correct Name")] CorrectName,   // expected to be the first entry

        [Description("Wrong Case")] WrongCase,

        [Description("Table Name")] TableName,

        [Description("Fuzzy Name")] Fuzzy,

        [Description("Duplicate Extension")] DuplicateExtension,    // requires that a 'CorrectName' hit also exist

        [Description("Missing File")] Missing,
        
        [Description("Unknown Table File")] Unknown,    // supported file extension, but unable to be matched against a game/table
        
        [Description("Unsupported File Type")] Unsupported    // unsupported file type (extension).. and thus also can't be matched against a game/table
    }
}