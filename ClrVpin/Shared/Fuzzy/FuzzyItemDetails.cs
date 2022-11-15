using ClrVpin.Models.Shared.Game;

namespace ClrVpin.Shared.Fuzzy;

public class FuzzyItemNameDetails
{
    public string ActualName { get; protected init; }
    public string Name { get; protected init;} // lower case
    public string ActualNameTrimmed { get; protected init;}
    public string NameWithoutWhiteSpace { get; protected init;}
    public string NameWithoutParenthesis { get; protected init;}
}

public class FuzzyItemDetails : FuzzyItemNameDetails
{
    public FuzzyItemDetails(string actualName, string actualNameTrimmed, string name, string nameWithoutWhiteSpace, string nameWithoutParenthesis, string manufacturer, string manufacturerNoWhiteSpace, int? year)
    {
        ActualName = actualName;
        ActualNameTrimmed = actualNameTrimmed;
        Name = name;
        NameWithoutWhiteSpace = nameWithoutWhiteSpace;
        NameWithoutParenthesis = nameWithoutParenthesis;

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