using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Xml.Serialization;
using ClrVpin.Shared;

namespace ClrVpin.Models.Shared.Database;

// view model info..
public class Game : GameBase
{
    [XmlIgnore]
    [JsonIgnore]
    public GameDerived Derived { get; } = new GameDerived();

    [XmlIgnore]
    [JsonIgnore]
    public bool IsExpanded { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public bool IsSelected { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public Fuzzy.FuzzyNameDetails FuzzyTableDetails { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public Fuzzy.FuzzyNameDetails FuzzyDescriptionDetails { get; set; }

    // Content contains 1 or more content hits (e.g. launch audio, wheel, etc), each of which can contain multiple media file hits (e.g. wrong case, valid, etc)
    [XmlIgnore]
    [JsonIgnore]
    public Content Content { get; } = new Content();

    [XmlIgnore]
    [JsonIgnore]
    public string TableFileWithExtension => Name + ".vpx";

    [XmlIgnore]
    [JsonIgnore]
    public ICommand NavigateToIpdbCommand { get; set; }

    public string GetContentName(ContentTypeCategoryEnum category) =>
        // determine the correct name - different for media vs pinball
        category == ContentTypeCategoryEnum.Media ? Description : Name;

    public override string ToString() => $"Table: {TableFileWithExtension}, IsSmelly: {Content?.IsSmelly}";

}