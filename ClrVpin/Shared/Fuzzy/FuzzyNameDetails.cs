using ClrVpin.Models.Shared.Game;

namespace ClrVpin.Shared.Fuzzy;

public class FuzzyNameDetails
{
    public FuzzyNameDetails(string name, string nameNoWhiteSpace, string manufacturer, int? year, string actualName)
    {
        Name = name;
        NameNoWhiteSpace = nameNoWhiteSpace;
        Manufacturer = manufacturer;
        Year = year;
        ActualName = actualName;
        IsOriginal = GameDerived.CheckIsOriginal(manufacturer, name);
    }

    public string Name { get; }
    public string NameNoWhiteSpace { get; }
    public string Manufacturer { get; set; }
    public int? Year { get; set; }
    public string ActualName { get; set; }
    public bool IsOriginal { get; }
}