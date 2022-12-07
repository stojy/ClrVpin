using System.Xml.Serialization;
using PropertyChanged;

namespace ClrVpin.Models.Shared.Database;

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

    [XmlIgnore]
    public string DatabaseFile { get; set; }
}