using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows.Input;
using ClrVpin.Models.Shared.Game;
using PropertyChanged;

namespace ClrVpin.Models.Importer.Vps;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
[AddINotifyPropertyChangedInterface]
public class OnlineGameBase
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Manufacturer { get; set; }
    public int Year { get; set; }
    public string Type { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastCreatedAt { get; set; }

    public string IpdbUrl { get; set; }
    public bool Broken { get; set; }
    public string[] Designers { get; set; }

    [JsonPropertyName("theme")]
    public string[] Themes { get; set; } = Array.Empty<string>();

    public int? Players { get; set; }
    public string[] Features { get; set; }
    public string Mpu { get; set; }

    public string ImgUrl { get; set; }

    public List<TableFile> TableFiles { get; set; } = new List<TableFile>();
    public List<ImageFile> B2SFiles { get; set; } = new List<ImageFile>();
    public List<File> WheelArtFiles { get; set; } = new List<File>();
    public List<File> RomFiles { get; set; } = new List<File>();
    public List<File> MediaPackFiles { get; set; } = new List<File>();
    public List<File> AltColorFiles { get; set; } = new List<File>();
    public List<File> SoundFiles { get; set; } = new List<File>();
    public List<File> TopperFiles { get; set; } = new List<File>();
    public List<File> PupPackFiles { get; set; } = new List<File>();
    public List<File> PovFiles { get; set; } = new List<File>();
    public List<File> AltSoundFiles { get; set; } = new List<File>();
    public List<File> RuleFiles { get; set; } = new List<File>();
    
    public override string ToString() => $"{Name} ({Manufacturer} {Year}), Tables={TableFiles.Count}, B2Ss={B2SFiles.Count}, Wheels={WheelArtFiles.Count}";
}

public class GameHit
{
    public GameDetail GameDetail { get; set; }
    public int? Score { get; set; }
}

public class FileCollection : List<File>
{
    public FileCollection(IEnumerable<File> files)
    {
        AddRange(files);
    }

    public bool IsNew { get; set; }
}

// view model
public class UrlSelection
{
    public string Url { get; set; }
    public ICommand SelectedCommand { get; set; }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class UrlDetail
{
    public bool Broken { get; set; }
    public string Url { get; set; }

    // view model
    public ICommand SelectedCommand { get; set; }
    public bool IsNew { get; set; }
}

public class File
{
    public string Name { get; set; }
    public string Version { get; set; }
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - setter required for deserialization
    public string[] Authors { get; set; } = Array.Empty<string>();

    public DateTime? UpdatedAt { get; set; }
    public DateTime? CreatedAt { get; set; }

    public UrlDetail[] Urls { get; set; }

    // view model properties
    public bool IsNew { get; set; }
}

public class ImageFile : File
{
    public string ImgUrl { get; set; }
    public string[] Features { get; set; }

    // view model properties
    public UrlSelection ImageUrlSelection { get; set; }
}

// ReSharper disable ClassNeverInstantiated.Global - required for collections as r# doesn't realize this is a json deserialized object
public class TableFile : ImageFile
{
    [JsonPropertyName("theme")]
    public string[] Themes { get; set; }

    public string TableFormat { get; set; }
    public string Comment { get; set; }
}