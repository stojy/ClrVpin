using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Xml.Serialization;
using ClrVpin.Shared;

namespace ClrVpin.Models.Shared.Database;

// view model info..
public class Game : GameBase
{
    // Content contains 1 or more content hits (e.g. launch audio, wheel, etc), each of which can contain multiple media file hits (e.g. wrong case, valid, etc)
    [XmlIgnore]
    public Content Content { get; } = new Content();

    [XmlIgnore]
    public string TableFileWithExtension => Name + ".vpx";

    [XmlIgnore]
    public int Number { get; set; }

    [XmlIgnore]
    public string Ipdb { get; set; }

    [XmlIgnore]
    public string IpdbUrl { get; set; }

    [XmlIgnore]
    public bool IsExpanded { get; set; }

    [XmlIgnore]
    public bool IsSelected { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public ICommand NavigateToIpdbCommand { get; set; }

    [XmlIgnore]
    public Fuzzy.FuzzyNameDetails FuzzyTableDetails { get; set; }

    [XmlIgnore]
    public Fuzzy.FuzzyNameDetails FuzzyDescriptionDetails { get; set; }

    [XmlIgnore]
    public bool IsOriginal => TableUtils.IsOriginal(Manufacturer);

    public string GetContentName(ContentTypeCategoryEnum category) =>
        // determine the correct name - different for media vs pinball
        category == ContentTypeCategoryEnum.Media ? Description : Name;

    public override string ToString() => $"Table: {TableFileWithExtension}, IsSmelly: {Content?.IsSmelly}";
}