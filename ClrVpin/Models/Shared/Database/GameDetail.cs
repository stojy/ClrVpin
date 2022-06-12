using System.Text.Json.Serialization;
using ClrVpin.Shared;
using ClrVpin.Shared.Fuzzy;
using PropertyChanged;

namespace ClrVpin.Models.Shared.Database;

[AddINotifyPropertyChangedInterface]
public class GameDetail
{
    // raw deserialized database entry
    public Game Game { get; set; }

    [JsonIgnore] // optimisation - no need to serialize this property, e.g. not required by DatabaseItem
    public GameDerived Derived { get; set; } = new GameDerived();

    [JsonIgnore] // optimisation - no need to serialize this property, e.g. not required by DatabaseItem
    public FuzzyDetails Fuzzy { get; } = new FuzzyDetails();

    [JsonIgnore] // optimisation - no need to serialize this property, e.g. not required by DatabaseItem
    // Content contains 1 or more content hits (e.g. launch audio, wheel, etc), each of which can contain multiple media file hits (e.g. wrong case, valid, etc)
    public Content Content { get; } = new Content();

    [JsonIgnore] // optimisation - no need to serialize this property, e.g. not required by DatabaseItem
    public ViewState ViewState { get; } = new ViewState();

    public override string ToString() => $"Table: {Derived.TableFileWithExtension}, IsSmelly: {Content?.IsSmelly}";
}