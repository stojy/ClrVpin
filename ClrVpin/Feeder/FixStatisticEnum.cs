using System.ComponentModel;

namespace ClrVpin.Feeder;

public enum FixStatisticsEnum
{
    [Description("Invalid Characters - Name")]
    NameInvalidCharacters,

    [Description("Invalid Characters - Manufacturer")]
    ManufacturerInvalidCharacters,

    [Description("Whitespace - Name")]
    NameWhitespace,

    [Description("Whitespace - Manufacturer")]
    ManufacturerWhitespace,

    [Description("Missing Image")]
    MissingImage,

    [Description("Manufacturered Includes Author")]
    ManufacturedIncludesAuthor,

    [Description("Original Table Includes IPDB")]
    OriginalTableIncludesIpdbUrl,

    [Description("Created Time - Too Low")]
    CreatedTimeLastTimeTooLow,

    [Description("Updated Time - Too Low")]
    UpdatedTimeTooLow,

    [Description("Updated Time - Too High")]
    UpdatedTimeTooHigh,

    [Description("Updated Time - Ordering")]
    UpdatedTimeOrdering,

    [Description("Updated Time - Less Than Created")]
    UpdatedTimeLessThanCreated,

    [Description("Upgrade Url to Https")]
    UpgradeUrlHttps,

    [Description("Invalid URL - IPDB")]
    InvalidUrlIpdb,
    
    [Description("Invalid URL - Content")]
    InvalidUrlContent,

    [Description("Wrong URL - IPDB")]
    WrongUrlIpdb,
    
    [Description("Wrong URL - Content")]
    WrongUrlContent,

    [Description("Wrong Name")]
    WrongName,

    [Description("Wrong Manufacturer/Year")]
    WrongManufacturerYear,

    [Description("Wrong Type")]
    WrongType,

    [Description("Duplicate Table")]
    DuplicateGame
}