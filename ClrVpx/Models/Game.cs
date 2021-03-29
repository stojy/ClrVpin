using System.Xml.Serialization;

namespace ClrVpx.Models
{
    public class Game
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }
        
        [XmlElement("rom")]
        public string Rom { get; set; }

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
 
        [XmlElement("ipdbNr")]
        public string IpdbNr { get; set; }

        [XmlElement("dateadded")]
        public string DateAdded { get; set; }

        [XmlElement("datemodified")]
        public string DateModified { get; set; }
    }
}