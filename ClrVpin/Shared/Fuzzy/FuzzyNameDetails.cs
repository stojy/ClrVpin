using ClrVpin.Models.Shared.Game;

namespace ClrVpin.Shared.Fuzzy;

public class FuzzyNameDetails
{
    public FuzzyNameDetails(string actualName, string nameOriginalCase, string name, string nameNoWhiteSpace, string manufacturer, string manufacturerNoWhiteSpace, int? year)
    {
        ActualName = actualName;
        NameOriginalCase = nameOriginalCase;
        Name = name;
        NameNoWhiteSpace = nameNoWhiteSpace;

        Manufacturer = manufacturer;
        ManufacturerNoWhiteSpace = manufacturerNoWhiteSpace;
        IsOriginal = GameDerived.CheckIsOriginal(manufacturer, name);

        Year = year;
    }

    public string Name { get; }
    public string NameOriginalCase { get; }
    public string NameNoWhiteSpace { get; }
    public string Manufacturer { get; set; }
    public string ManufacturerNoWhiteSpace { get; set; }
    public int? Year { get; set; }
    public string ActualName { get; set; }
    public bool IsOriginal { get; }
}