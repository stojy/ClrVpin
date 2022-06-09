using System.Xml.Serialization;
using PropertyChanged;

namespace ClrVpin.Models.Shared.Database;

[AddINotifyPropertyChangedInterface]
public class GameBase
{
    [XmlAttribute("name")]
    public string Name { get; set; } // used by VPX (table, b2s, and pov - filename must match this property.  Refer GetContentName

    [XmlElement("description")]
    public string Description { get; set; } // used by frontends (pbx/pby) - filename must match this property.  Refer GetContentName

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
    public string Rating { get; set; }

    [XmlElement("players")]
    public string Players { get; set; }

    [XmlElement("comment")]
    public string Comment { get; set; }

    [XmlElement("theme")]
    public string Theme { get; set; }

    [XmlElement("author")]
    public string Author { get; set; }

    [XmlElement("version")]
    public string Version { get; set; }

    [XmlElement("ipdbid")]
    public string IpdbId { get; set; }

    [XmlIgnore] // read property if it exists, but don't write it back during serialization. support kept here for PinballX Manager which (i believe) uses this field
    [XmlElement("ipdbNr")]
    public string IpdbNr { get; set; }

    [XmlElement("dateadded")]
    public string DateAddedString { get; set; }

    [XmlElement("datemodified")]
    public string DateModifiedString { get; set; }
}