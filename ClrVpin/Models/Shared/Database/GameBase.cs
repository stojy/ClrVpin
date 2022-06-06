using System;
using System.Xml.Serialization;
using Microsoft.VisualBasic.CompilerServices;
using PropertyChanged;

namespace ClrVpin.Models.Shared.Database;

[AddINotifyPropertyChangedInterface]
public class GameBase
{
    [XmlAttribute("name")]
    public string Name { get; set; } // used by VPX (table, b2s, and pov - filename must match this property.  Refer GetContentName

    [XmlElement("description", IsNullable = true)]
    public string Description { get; set; } // used by frontends (pbx/pby) - filename must match this property.  Refer GetContentName

    [XmlElement("rom", IsNullable = true)]
    public string Rom { get; set; }

    [XmlElement("manufacturer", IsNullable = true)]
    public string Manufacturer { get; set; }

    [XmlElement("year", IsNullable = true)]
    public string Year { get; set; }

    [XmlElement("type", IsNullable = true)]
    public string Type { get; set; }

    [XmlElement("hidedmd", IsNullable = true)]
    public string HideDmd { get; set; }

    [XmlElement("hidetopper", IsNullable = true)]
    public string HideTopper { get; set; }

    [XmlElement("hidebackglass", IsNullable = true)]
    public string HideBackglass { get; set; }

    [XmlElement("enabled", IsNullable = true)]
    public string Enabled { get; set; }

    [XmlElement("rating", IsNullable = true)]
    public string Rating { get; set; }

    [XmlElement("players", IsNullable = true)]
    public string Players { get; set; }

    [XmlElement("comment", IsNullable = true)]
    public string Comment { get; set; }

    [XmlElement("theme", IsNullable = true)]
    public string Theme { get; set; }

    [XmlElement("author", IsNullable = true)]
    public string Author { get; set; }

    [XmlElement("version", IsNullable = true)]
    public string Version { get; set; }

    [XmlElement("ipdbid", IsNullable = true)]
    public string IpdbId { get; set; }

    [XmlElement("ipdbNr", IsNullable = true)]
    public string IpdbNr { get; set; }

    [XmlElement("dateadded", IsNullable = true)]
    public string DateAddedString { get; set; }

    [XmlElement("datemodified", IsNullable = true)]
    public string DateModifiedString { get; set; }
}