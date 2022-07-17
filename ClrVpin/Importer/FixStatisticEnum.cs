using System.ComponentModel;

namespace ClrVpin.Importer;

public enum FixStatisticsEnum
{
    [Description("Table Name Whitespace")]
    NameWhitespace,

    [Description("Table Missing Image Url")]
    MissingImage,

    [Description("Table Manufacturer Whitespace")]
    ManufacturerWhitespace,

    [Description("Manufacturered Contains Author")]
    ManufacturedContainsAuthor,

    [Description("Table Wrong Manufacturer")]
    WrongManufacturer,

    [Description("Table Wrong Name")]
    WrongName,

    [Description("Table Created Time")]
    TableCreatedTime,

    [Description("Table Updated Time Too Low")]
    TableUpdatedTimeTooLow,

    [Description("Table Updated Time Too High")]
    TableUpdatedTimeTooHigh,

    [Description("File Update Time Ordering")]
    FileUpdateTimeOrdering,

    [Description("File Updated Time")]
    FileUpdatedTime,

    [Description("Invalid Url")]
    InvalidUrl,

    [Description("Wrong Url")]
    WrongUrl,

    [Description("Invalid IPDB Url")]
    InvalidIpdbUrl,

    [Description("Wrong IPDB Url")]
    WrongIpdbUrl,

    [Description("Duplicate Table")]
    DuplicateGame
}