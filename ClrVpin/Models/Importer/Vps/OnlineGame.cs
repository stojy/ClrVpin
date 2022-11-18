using System.Collections.Generic;

namespace ClrVpin.Models.Importer.Vps;

// ReSharper disable ClassNeverInstantiated.Global - required for collections as r# doesn't realize this is a json deserialized object

// todo; move this to a 'Derived' object.. similar to Game
public class OnlineGame : OnlineGameBase
{
    // view model properties
    public Dictionary<string, FileCollection> AllFiles { get; set; }
    public List<FileCollection> AllFilesList { get; set; }
    public IEnumerable<File> AllFilesFlattenedList { get; set; }
    public List<ImageFile> ImageFiles { get; set; }

    // main image url - shown between the HW and SW info panels
    public UrlSelection ImageUrlSelection { get; set; }

    public string YearString { get; set; }
    public TableAvailabilityOptionEnum TableAvailability { get; set; }
    public List<string> TableFormats { get; set; }

    public bool IsOriginal { get; set; }

    public string IpdbId { get; set; } = string.Empty;
    public string Description { get; set; }

    public string VpsUrl { get; set; }

    // reference to the highest fuzzy ranked DB match
    public GameHit Hit { get; set; }

    public TableNewContentOptionEnum? NewContentType {get; set; }
    
    public string CreateDescription() => $"{Name?.Trim()} ({Manufacturer?.Trim()} {Year})";
}