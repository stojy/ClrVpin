using System.ComponentModel;

namespace ClrVpin.Models.Importer
{
    public enum FixFeedOptionEnum
    {
        [Description("Whitespace")] Whitespace,
        [Description("Missing Image")] MissingImageUrl,
        [Description("Manufactured Table Includes Author")] ManufacturedTableContainsAuthor,
        [Description("Created Time")] CreatedTime,
        [Description("Updated Time")] UpdatedTime,
        [Description("Invalid URL")] InvalidUrl,
        [Description("Wrong URL - Content")] WrongContentUrl,
        [Description("Wrong URL - IPDB")] WrongIpdbUrl,
        [Description("Wrong Manufacturer & Year")] WrongManufacturerAndYear,
        [Description("Wrong Table")] WrongName,
        [Description("Duplicate Table")] DuplicateTable,
    }
}