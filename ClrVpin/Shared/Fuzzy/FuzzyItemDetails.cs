using ClrVpin.Models.Shared.Game;

namespace ClrVpin.Shared.Fuzzy;

public class FuzzyItemNameDetails
{
    public string ActualName { get; protected init; }
    public string ActualNameTrimmed { get; protected init;}
    public string ActualNameWithoutManufacturerOrYear { get; protected init; }
    public string Name { get; protected init;} // lower case
    public string NameWithoutWhiteSpace { get; protected init;}
    public string NameWithoutParenthesis { get; protected init;}
}

public class FuzzyItemDetails : FuzzyItemNameDetails
{

    public FuzzyItemDetails(string actualName, string actualNameTrimmed, string actualNameWithoutManufacturerOrYear, string name, string nameWithoutWhiteSpace, string nameWithoutParenthesis, 
        string manufacturer, string manufacturerNoWhiteSpace, int? year)
    {
        // original name, e.g. proper case
        ActualName = actualName;
        ActualNameTrimmed = actualNameTrimmed;
        ActualNameWithoutManufacturerOrYear = actualNameWithoutManufacturerOrYear;
     
        // cleansed name, e.g. lower case
        Name = name;
        NameWithoutWhiteSpace = nameWithoutWhiteSpace;
        NameWithoutParenthesis = nameWithoutParenthesis;

        // manufacturer in lower case
        Manufacturer = manufacturer;
        ManufacturerNoWhiteSpace = manufacturerNoWhiteSpace;
        
        IsOriginal = GameDerived.CheckIsOriginal(manufacturer, name);

        Year = year;
    }

    public string Manufacturer { get; set; } // lower case
    public string ManufacturerNoWhiteSpace { get; }
    public int? Year { get; set; }
    public bool IsOriginal { get; }
}