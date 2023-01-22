using System.Text.Json.Serialization;
using System.Windows.Input;
using ClrVpin.Models.Feeder.Vps;
using ClrVpin.Shared;
using ClrVpin.Shared.Fuzzy;
using PropertyChanged;

namespace ClrVpin.Models.Shared.Game;

[AddINotifyPropertyChangedInterface]
public class LocalGame
{
    public void Init(int? number = null)
    {
        GameDerived.Init(this, number);
        
        // assign fuzzy name details before they are used to to avoid need for re-calculate multiple times later on, e.g. when comparing against EACH of the file matches
        FuzzyDetails.UpdateLocalGameFuzzyDetails(this);
    }

    // raw deserialized database entry
    public Database.Game Game { get; set; }

    [JsonIgnore] // optimisation - no need to serialize this property, e.g. not required by DatabaseItem
    public GameDerived Derived { get; set; } = new();

    [JsonIgnore] // optimisation - no need to serialize this property, e.g. not required by DatabaseItem
    public FuzzyDetails Fuzzy { get; } = new();

    [JsonIgnore] // optimisation - no need to serialize this property, e.g. not required by DatabaseItem
    // Content contains 1 or more content hits (e.g. launch audio, wheel, etc), each of which can contain multiple media file hits (e.g. wrong case, valid, etc)
    public Content Content { get; } = new();

    [JsonIgnore] // optimisation - no need to serialize this property, e.g. not required by DatabaseItem
    public ViewState ViewState { get; } = new();

    [JsonIgnore] // optimisation - no need to serialize this property, e.g. not required by DatabaseItem
    // ReSharper disable once UnusedAutoPropertyAccessor.Global - keeping for future use
    public OnlineGame OnlineGame { get; set; }
    
    // VM properties
    [JsonIgnore] // optimisation - no need to serialize this property, e.g. not required by DatabaseItem
    public ICommand UpdateDatabaseEntryCommand { get; set; }

    public override string ToString() => $"Table: {Derived.TableFileWithExtension}, IsSmelly: {Content?.IsSmelly}";
}