using System.Collections.Generic;
using System.Xml.Serialization;

namespace ClrVpin.Models
{
    [XmlRoot("menu")]
    public class Menu
    {
        [XmlElement("game")]
        public List<Game> Games { get; set; }
    }
}