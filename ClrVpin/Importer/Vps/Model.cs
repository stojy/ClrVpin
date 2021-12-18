using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Utils;

namespace ClrVpin.Importer.Vps;

// ReSharper disable ClassNeverInstantiated.Global - required for collections as r# doesn't realize this is a json deserialized object
public class Game
{
    public int Index { get; set; }
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
    public string[] Themes { get; set; }

    public int? Players { get; set; }
    public string[] Features { get; set; }
    public string Mpu { get; set; }
    public string ImgUrl { get; set; }

    public TableFiles TableFiles { get; set; }
    public ImageFile[] B2SFiles { get; set; }
    public File[] WheelArtFiles { get; set; }
    public File[] RomFiles { get; set; }

    public File[] PovFiles { get; set; }
    public File[] MediaPackFiles { get; set; }
    public File[] SoundFiles { get; set; }
    public File[] TopperFiles { get; set; }
    public File[] PupPackFiles { get; set; }
    public File[] AltColorFiles { get; set; }
    public File[] AltSoundFiles { get; set; }
    public File[] RuleFiles { get; set; }
}

public class TableFiles : List<TableFile> { }

public class UrlDetail
{
    public bool Broken { get; set; }
    public string Url { get; set; }
}

public class File
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string[] Authors { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public DateTime? CreatedAt { get; set; }

    public UrlDetail[] Urls { get; set; }
}

public class ImageFile : File
{
    public string ImgUrl { get; set; }
    public string[] Features { get; set; }
}

public class TableFile : ImageFile
{
    [JsonPropertyName("theme")]
    public string[] Themes { get; set; }

    public string TableFormat { get; set; }
    public string Comment { get; set; }
}