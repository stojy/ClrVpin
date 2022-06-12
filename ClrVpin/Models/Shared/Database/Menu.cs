using System.Collections.Generic;
using System.Xml.Serialization;

namespace ClrVpin.Models.Shared.Database
{
    [XmlRoot("menu")]
    public class Menu
    {
        [XmlElement("game")]
        public List<GameDetail> Games { get; set; }
    }
}