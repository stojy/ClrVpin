using System.ComponentModel;

namespace ClrVpin.Models.Importer
{
    public enum FeedFixOptionEnum
    {
        [Description("Whitespace")] Whitespace,
        [Description("Missing Image")] MissingImageUrl,
        [Description("Manufactured Table Author")] ManufacturedTableContainsAuthor,
        [Description("Created Time")] CreatedTime,
        [Description("Updated Time")] UpdatedTime,
        [Description("Invalid URL")] InvalidUrl,
        [Description("Wrong Manufacturer")] WrongManufacturer,
        [Description("Wrong Table")] WrongTable,
        [Description("Wrong Content URL")] WrongContentUrl,
        [Description("Wrong IPDB URL")] WrongIpdbUrl,
        [Description("Duplicate Table")] DuplicateTable,
    }
}