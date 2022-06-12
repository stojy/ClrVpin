using System.Text.Json.Serialization;
using System.Xml.Serialization;
using ClrVpin.Shared;
using ClrVpin.Shared.Fuzzy;
using PropertyChanged;

namespace ClrVpin.Models.Shared.Database;

[AddINotifyPropertyChangedInterface]
public class GameDetail : Game
{
    [XmlIgnore]
    [JsonIgnore]
    public GameDerived Derived { get; } = new GameDerived();

    [XmlIgnore]
    [JsonIgnore]
    public FuzzyDetails Fuzzy { get; } = new FuzzyDetails();

    [XmlIgnore]
    [JsonIgnore]
    // Content contains 1 or more content hits (e.g. launch audio, wheel, etc), each of which can contain multiple media file hits (e.g. wrong case, valid, etc)
    public Content Content { get; } = new Content();

    [XmlIgnore]
    [JsonIgnore]
    public ViewState ViewState { get; } = new ViewState();


    public override string ToString() => $"Table: {Derived.TableFileWithExtension}, IsSmelly: {Content?.IsSmelly}";
}