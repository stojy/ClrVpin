using ClrVpin.Models.Shared.Game;

namespace ClrVpin.Shared.Fuzzy;

public class FuzzyItemDetails
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

    public string Name { get; }
    public string NameOriginalCase { get; }
    public string NameWithoutWhiteSpace { get; }
    public string NameWithoutParenthesis { get; }
    public string Manufacturer { get; set; }
    public string ManufacturerNoWhiteSpace { get; }
    public int? Year { get; set; }
    public string ActualName { get; }
    public bool IsOriginal { get; }
}