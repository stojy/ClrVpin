using ClrVpin.Models.Shared.Game;

namespace ClrVpin.Shared.Fuzzy;

public class FuzzyItemNameDetails
{
    public string ActualName { get; protected init; }
    public string Name { get; protected init;}
    public string NameOriginalCase { get; protected init;}
    public string NameWithoutWhiteSpace { get; protected init;}
    public string NameWithoutParenthesis { get; protected init;}
}

public class FuzzyItemDetails : FuzzyItemNameDetails
{
    public FuzzyItemDetails(string actualName, string nameOriginalCase, string name, string nameWithoutWhiteSpace, string nameWithoutParenthesis, string manufacturer, string manufacturerNoWhiteSpace, int? year)
    {
        ActualName = actualName;
        NameOriginalCase = nameOriginalCase;
        Name = name;
        NameWithoutWhiteSpace = nameWithoutWhiteSpace;
        NameWithoutParenthesis = nameWithoutParenthesis;

        Manufacturer = manufacturer;
        ManufacturerNoWhiteSpace = manufacturerNoWhiteSpace;
        
        IsOriginal = GameDerived.CheckIsOriginal(manufacturer, name);

        Year = year;
    }

    public string Manufacturer { get; set; }
    public string ManufacturerNoWhiteSpace { get; }
    public int? Year { get; set; }
    public bool IsOriginal { get; }
}