using System.Collections.Generic;
using System.Windows.Input;
using ClrVpin.Shared;

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
    public bool IsOriginal => TableUtils.IsOriginal(Manufacturer);

    // reference to the highest fuzzy ranked DB match
    public GameHit Hit { get; set; }
    public bool IsMatched => Hit != null;

    public ICommand ViewDatabaseEntryCommand { get; set; }
    public ICommand AddDatabaseEntryCommand { get; set; }
}