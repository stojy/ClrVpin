using System.ComponentModel;

namespace ClrVpin.Models.Feeder;

public enum FixFeedOptionEnum
{
    [Description("Invalid Characters")] InvalidCharacters,
    [Description("Whitespace")] Whitespace,
    [Description("Missing Image")] MissingImageUrl,
    [Description("Manufactured Table Includes Author")] ManufacturedIncludesAuthor,
    [Description("Original Table Includes IPDB URL")] OriginalTableIncludesIpdbUrl,
    [Description("Created Time")] CreatedTime,
    [Description("Updated Time")] UpdatedTime,
    [Description("Upgrade URL to https")] UpgradeUrlHttps,
    [Description("Invalid URL - Content")] InvalidUrlContent,
    [Description("Invalid URL - IPDB")] InvalidUrlIpdb,
    [Description("Wrong URL - Content")] WrongUrlContent,
    [Description("Wrong URL - IPDB")] WrongUrlIpdb,
    [Description("Wrong Name")] WrongName,
    [Description("Wrong Manufacturer/Year")] WrongManufacturerYear,
    [Description("Wrong Type")] WrongType,
    [Description("Duplicate Table")] DuplicateTable,
    [Description("Wrong Simulator")] WrongSimulator
}