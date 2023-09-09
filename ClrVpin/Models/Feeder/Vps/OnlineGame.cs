using System;
using System.Collections.Generic;

namespace ClrVpin.Models.Feeder.Vps;


// todo; move this to a 'Derived' object.. similar to LocalGame
[Serializable]
public class OnlineGame : OnlineGameBase
{
    // view model properties
    public Dictionary<string, FileCollection> AllFileCollections { get; set; }
    public List<FileCollection> AllFileCollectionsList { get; set; }
    public IEnumerable<File> AllFilesFlattenedList { get; set; }
    public List<ImageFile> ImageFiles { get; set; }

    // main image url - shown between the HW and SW info panels
    public UrlSelection ImageUrlSelection { get; set; }

    public string YearString { get; set; }
    public TableDownloadOptionEnum TableDownload { get; set; }
    public List<string> TableFormats { get; set; }

    public bool IsOriginal { get; set; }
    public TableStyleOptionEnum TableStyleOption { get; set; }

    public string IpdbId { get; set; } = string.Empty;
    public string Description { get; set; }

    public string VpsUrl { get; set; }

    // reference to the highest fuzzy ranked DB match
    public LocalGameHit Hit { get; set; }

    public List<string> IsNewFileCollectionTypes { get; set; } = new();
    
    public string CreateDescription() => $"{Name?.Trim()} ({Manufacturer?.Trim()} {Year})";
}