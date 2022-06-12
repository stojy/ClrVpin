using System;
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
    public int Number { get; private set; }

    [XmlIgnore]
    [JsonIgnore]
    public string Ipdb { get; private set; }

    [XmlIgnore]
    [JsonIgnore]
    public string IpdbUrl { get; private set; }

    [XmlIgnore]
    [JsonIgnore]
    public string NameLowerCase { get; private set; }

    [XmlIgnore]
    [JsonIgnore]
    public string DescriptionLowerCase { get; private set; }

    [XmlIgnore]
    [JsonIgnore]
    public bool IsOriginal { get; private set; }

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

    public static void UpdateDerivedProperties(Game game, int? number = null)
    {
        game.Number = number ?? game.Number;

        game.IsOriginal = CheckIsOriginal(game.Manufacturer);

        if (game.IsOriginal)
        {
            game.Ipdb = null;
            game.IpdbUrl = null;
            game.IpdbNr = null;

            // don't assign null as this will result in the tag being removed from serialization.. which is valid, but inconsistent with the original xml file that always defines <ipdbid>
            game.IpdbId = "";
        }
        else
        {
            game.Ipdb = game.IpdbId ?? game.IpdbNr ?? game.Ipdb;
            game.IpdbUrl = game.Ipdb == null ? null : $"https://www.ipdb.org/machine.cgi?id={game.Ipdb}";
        }

        // memory optimisation to perform this operation once on database read instead of multiple times during fuzzy comparison (refer Fuzzy.GetUniqueMatch)
        game.NameLowerCase = game.Name.ToLower();
        game.DescriptionLowerCase = game.Description.ToLower();
    }

    // assign isOriginal based on the manufacturer
    public static bool CheckIsOriginal(string manufacturer) => manufacturer?.StartsWith("Original", StringComparison.InvariantCultureIgnoreCase) == true ||
                                                               manufacturer?.StartsWith("Zen Studios", StringComparison.InvariantCultureIgnoreCase) == true;
}