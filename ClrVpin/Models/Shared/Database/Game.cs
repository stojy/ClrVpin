using System.Xml.Serialization;
using PropertyChanged;

namespace ClrVpin.Models.Shared.Database;

// unlike PinballX, PinballY will NOT discard unknown properties (e.g. newer properties that are not supported)
[AddINotifyPropertyChangedInterface]
public class Game
{
    [XmlAttribute("name")]
    public string Name { get; set; } // used by VPX (table, b2s, and pov - filename must match this property.  Refer GetName

    [XmlElement("description")]
    public string Description { get; set; } = ""; // used by frontends (pbx/pby) - filename must match this property.  Refer GetName

    [XmlElement("rom")]
    public string Rom { get; set; }

    [XmlElement("manufacturer")]
    public string Manufacturer { get; set; }

    [XmlElement("year")]
    public string Year { get; set; }

    [XmlElement("type")]
    public string Type { get; set; }

    [XmlElement("hidedmd")]
    public string HideDmd { get; set; }

    [XmlElement("hidetopper")]
    public string HideTopper { get; set; }

    [XmlElement("hidebackglass")]
    public string HideBackglass { get; set; }

    [XmlElement("enabled")]
    public string Enabled { get; set; }

    [XmlElement("rating")]
    public double? Rating { get; set; }

    // don't serialize a null Rating, otherwise the XmlSerializer behavior is to serialize null as 'xsi:nil="true"'
    // - refer https://stackoverflow.com/a/246359/227110
    public bool ShouldSerializeRating() => Rating.HasValue;

    // NOT supported by PinballX "Game Manager" - the value will be discarded when the file is written
    [XmlElement("players")]
    public string Players { get; set; }

    [XmlElement("comment")]
    public string Comment { get; set; }

    // NOT supported by PinballX "Game Manager" - the value will be discarded when the file is written
    // NOT supported by PinballX "Database Manager" - the value will be discarded when the file is written
    [XmlElement("theme")]
    public string Theme { get; set; }

    // NOT supported by PinballX "Game Manager" - the value will be discarded when the file is written
    [XmlElement("author")]
    public string Author { get; set; }

    // NOT supported by PinballX "Game Manager" - the value will be discarded when the file is written
    [XmlElement("version")]
    public string Version { get; set; }

    // NOT supported by PinballX "Game Manager" - the value will be discarded when the file is written
    // NOT supported by PinballX "Database Manager" - the value will be discarded when the file is written
    [XmlElement("ipdbid")]
    public string IpdbId { get; set; }

    [XmlIgnore] // read property if it exists, but don't write it back during serialization. support kept here for PinballX Manager which (i believe) uses this field
    [XmlElement("ipdbNr")]
    public string IpdbNr { get; set; }

    // NOT supported by PinballX "Game Manager" - the value will be discarded when the file is written
    [XmlElement("dateadded")]
    public string DateAddedString { get; set; }

    // NOT supported by PinballX "Game Manager" - the value will be discarded when the file is written
    [XmlElement("datemodified")]
    public string DateModifiedString { get; set; }

    // NOT supported by PinballX "Game Manager" - the value will be discarded when the file is written
    // NOT supported by PinballX "Database Manager" - the value will be discarded when the file is written
    [XmlElement("pup")]
    public string Pup{ get; set; }

    [XmlIgnore]
    public string DatabaseFile { get; set; }
}