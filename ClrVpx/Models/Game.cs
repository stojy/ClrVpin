using System;
using System.Xml.Serialization;

namespace ClrVpx.Models
{
    public class Game
    {
        [XmlAttribute("name")]
        public string Name { get; set; }    // table name - typically matches description, but not always

        [XmlElement("description", IsNullable = true)]
        public string Description { get; set; } // from IPDB - media files must match
        
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
        public string DateAdded { get; set; }

        [XmlElement("datemodified", IsNullable = true)]
        public string DateModified { get; set; }


        // calculated properties
        public int Number { get; set; }
        public string Ipdb { get; set; }
        public string TableAudio { get; set; }
        public int TableAudioMatchScore { get; set; }
    }
}