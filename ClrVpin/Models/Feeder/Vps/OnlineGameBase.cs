using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows.Input;
using ClrVpin.Models.Shared.Game;
using PropertyChanged;

namespace ClrVpin.Models.Feeder.Vps;

[AddINotifyPropertyChangedInterface]
[Serializable]
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

    public List<TableFile> TableFiles { get; set; } = new();
    public List<ImageFile> B2SFiles { get; set; } = new();
    public List<File> WheelArtFiles { get; set; } = new();
    public List<File> RomFiles { get; set; } = new();
    public List<File> MediaPackFiles { get; set; } = new();
    public List<File> AltColorFiles { get; set; } = new();
    public List<File> SoundFiles { get; set; } = new();
    public List<File> TopperFiles { get; set; } = new();
    public List<File> PupPackFiles { get; set; } = new();
    public List<File> PovFiles { get; set; } = new();
    public List<File> AltSoundFiles { get; set; } = new();
    public List<File> RuleFiles { get; set; } = new();
    
    public override string ToString() => $"{Name} ({Manufacturer} {Year}), Tables={TableFiles.Count}, B2Ss={B2SFiles.Count}, Wheels={WheelArtFiles.Count}";
}

[AddINotifyPropertyChangedInterface]
public class LocalGameHit
{
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public LocalGame LocalGame { get; set; }
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public int? Score { get; set; }
}

[AddINotifyPropertyChangedInterface]
[Serializable]
public class FileCollection : List<File>
{
    public FileCollection(IEnumerable<File> files)
    {
        AddRange(files);
    }

    public bool IsNew { get; set; }
    public string Title { get; set; }
    public UrlStatusEnum UrlStatus { get; set; }
    public bool IsNewAndSelectedFileType { get; set; }
}

// view model
[AddINotifyPropertyChangedInterface]
public class UrlSelection
{
    public string Url { get; set; }
    public ICommand SelectedCommand { get; set; }
}

// ReSharper disable once ClassNeverInstantiated.Global
[AddINotifyPropertyChangedInterface]
public class UrlDetail
{
    public bool Broken { get; set; }
    public string Url { get; set; }

    // view model
    public ICommand SelectedCommand { get; set; }
    public bool IsNew { get; set; }
}

[AddINotifyPropertyChangedInterface]
[Serializable]
public class File
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string[] Authors { get; set; } = Array.Empty<string>();

    public DateTime? UpdatedAt { get; set; }
    public DateTime? CreatedAt { get; set; }

    public UrlDetail[] Urls { get; set; } = Array.Empty<UrlDetail>();

    // view model properties
    public bool IsNew { get; set; }
    public UrlStatusEnum UrlStatusEnum { get; set; }
}

[AddINotifyPropertyChangedInterface]
[Serializable]
public class ImageFile : File
{
    public string ImgUrl { get; set; }
    public string[] Features { get; set; }

    // view model properties
    public UrlSelection ImageUrlSelection { get; set; }
    public bool IsFullDmd { get; set; }
}

// ReSharper disable ClassNeverInstantiated.Global - required for collections as r# doesn't realize this is a json deserialized object
[AddINotifyPropertyChangedInterface]
public class TableFile : ImageFile
{
    [JsonPropertyName("theme")]
    public string[] Themes { get; set; }

    public string TableFormat { get; set; }
    public string Comment { get; set; }
    
    // view model properties
    public bool IsVirtualOnly { get; set; }
    public bool IsFullSingleScreen { get; set; }
    public bool IsMusicOrSoundMod { get; set; }
    public bool IsBlackWhiteMod { get; set; }
    public SimulatorOptionEnum? Simulator { get; set; } // converted from TableFormat
}
