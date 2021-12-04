namespace ClrVpin.Importer.Vps;


public class Game
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string Manufacturer { get; set; }
    public int Year { get; set; }
    public string[] Theme { get; set; }
    public int Players { get; set; }
    public string[] Designers { get; set; }
    public string IpdbUrl { get; set; }
    public string Type { get; set; }
    public long UpdatedAt { get; set; }
    public Tablefile[] TableFiles { get; set; }
    public Povfile[] PovFiles { get; set; }
    public Wheelartfile[] WheelArtFiles { get; set; }
    public string[] Features { get; set; }
    public long LastCreatedAt { get; set; }
    public Mediapackfile[] MediaPackFiles { get; set; }
    public Soundfile[] SoundFiles { get; set; }
    public B2Sfiles[] B2SFiles { get; set; }
    public bool Broken { get; set; }
    public string Mpu { get; set; }
    public Romfile[] RomFiles { get; set; }
    public Topperfile[] TopperFiles { get; set; }
    public Puppackfile[] PupPackFiles { get; set; }
    public Altcolorfile[] AltColorFiles { get; set; }
    public Altsoundfile[] AltSoundFiles { get; set; }
    public Rulefile[] RuleFiles { get; set; }
    public string ImgUrl { get; set; }
}

public class Tablefile
{
    public UrlDetail[] Urls { get; set; }
    public string[] Theme { get; set; }
    public string Version { get; set; }
    public string[] Authors { get; set; }
    public long UpdatedAt { get; set; }
    public long CreatedAt { get; set; }
    public string TableFormat { get; set; }
    public string ImgUrl { get; set; }
    public string Comment { get; set; }
    public string[] Features { get; set; }
}

public class UrlDetail
{
    public bool Broken { get; set; }
    public string Url { get; set; }
}

public class Povfile
{
    public string Version { get; set; }
    public Url1[] Urls { get; set; }
    public string[] Authors { get; set; }
    public long UpdatedAt { get; set; }
}

public class Url1
{
    public string Url { get; set; }
    public bool Broken { get; set; }
}

public class Wheelartfile
{
    public Url2[] Urls { get; set; }
    public string Version { get; set; }
    public string[] Authors { get; set; }
    public long UpdatedAt { get; set; }
    public long CreatedAt { get; set; }
    public string Name { get; set; }
}

public class Url2
{
    public string Url { get; set; }
    public bool Broken { get; set; }
}

public class Mediapackfile
{
    public Url3[] Urls { get; set; }
    public string Version { get; set; }
    public string[] Authors { get; set; }
    public long UpdatedAt { get; set; }
    public long CreatedAt { get; set; }
    public string Name { get; set; }
}

public class Url3
{
    public string Url { get; set; }
    public bool Broken { get; set; }
}

public class Soundfile
{
    public Url4[] Urls { get; set; }
    public string Version { get; set; }
    public string[] Authors { get; set; }
    public long UpdatedAt { get; set; }
}

public class Url4
{
    public string Url { get; set; }
    public bool Broken { get; set; }
}

public class B2Sfiles
{
    public long CreatedAt { get; set; }
    public Url5[] Urls { get; set; }
    public long UpdatedAt { get; set; }
    public string[] Authors { get; set; }
    public string ImgUrl { get; set; }
    public string Version { get; set; }
    public string[] Features { get; set; }
}

public class Url5
{
    public string Url { get; set; }
    public bool Broken { get; set; }
}

public class Romfile
{
    public string Version { get; set; }
    public Url6[] Urls { get; set; }
    public string[] Authors { get; set; }
    public long UpdatedAt { get; set; }
    public long CreatedAt { get; set; }
    public string Name { get; set; }
}

public class Url6
{
    public string Url { get; set; }
    public bool Broken { get; set; }
}

public class Topperfile
{
    public Url7[] Urls { get; set; }
    public string Version { get; set; }
    public string[] Authors { get; set; }
    public long UpdatedAt { get; set; }
    public long CreatedAt { get; set; }
    public string Name { get; set; }
}

public class Url7
{
    public string Url { get; set; }
    public bool Broken { get; set; }
}

public class Puppackfile
{
    public string Name { get; set; }
    public string Version { get; set; }
    public Url8[] Urls { get; set; }
    public string[] Authors { get; set; }
    public long UpdatedAt { get; set; }
    public long CreatedAt { get; set; }
}

public class Url8
{
    public string Url { get; set; }
    public bool Broken { get; set; }
}

public class Altcolorfile
{
    public Url9[] Urls { get; set; }
    public string Version { get; set; }
    public string[] Authors { get; set; }
    public long UpdatedAt { get; set; }
    public long CreatedAt { get; set; }
    public string Name { get; set; }
}

public class Url9
{
    public string Url { get; set; }
    public bool Broken { get; set; }
}

public class Altsoundfile
{
    public Url10[] Urls { get; set; }
    public string Version { get; set; }
    public string[] Authors { get; set; }
    public long UpdatedAt { get; set; }
    public long CreatedAt { get; set; }
    public string Name { get; set; }
}

public class Url10
{
    public string Url { get; set; }
    public bool Broken { get; set; }
}

public class Rulefile
{
    public long CreatedAt { get; set; }
    public Url11[] Urls { get; set; }
    public long UpdatedAt { get; set; }
}

public class Url11
{
    public string Url { get; set; }
}
