using System.Collections.Generic;
using System.Xml.Serialization;

namespace ClrVpx.Models
{
    [XmlRoot("menu")]
    public class Menu
    {
        [XmlElement("game")]
        public List<Game> Games { get; set; }
    }
}