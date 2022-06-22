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
    }

    public string Name { get; }
    public string NameNoWhiteSpace { get; }
    public string Manufacturer { get; set; }
    public int? Year { get; set; }
    public string ActualName { get; set; }
}