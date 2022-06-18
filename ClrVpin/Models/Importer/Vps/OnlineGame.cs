using System.Collections.Generic;
using System.Windows.Input;
using ClrVpin.Models.Shared.Game;

namespace ClrVpin.Models.Importer.Vps;

// ReSharper disable ClassNeverInstantiated.Global - required for collections as r# doesn't realize this is a json deserialized object
public class OnlineGame : OnlineGameBase
{
    // view model properties
    public Dictionary<string, FileCollection> AllFiles { get; set; }
    public IEnumerable<File> AllFilesList { get; set; }
    public List<ImageFile> ImageFiles { get; set; }

    public int Index { get; set; }
    public UrlSelection ImageUrlSelection { get; set; }
    public string YearString { get; set; }
    public bool IsOriginal => GameDerived.CheckIsOriginal(Manufacturer, Name);

    public string IpdbId { get; set; } = string.Empty;

    // reference to the highest fuzzy ranked DB match
    public GameHit Hit { get; set; }
    public bool IsMatched { get; set; }

    public bool IsMatchingEnabled { get; set; }
    public string UpdateDatabaseEntryTooltip { get; set; } = "Update existing local database entry";
    public string CreateDatabaseEntryTooltip { get; set; } = "Create new local database entry";
    public ICommand UpdateDatabaseEntryCommand { get; set; }
    public ICommand CreateDatabaseEntryCommand { get; set; }
}