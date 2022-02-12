using System.Xml.Linq;
using System.Xml.Serialization;

namespace Utils.Extensions
{
    public static class XmlExtensions
    {
        public static T Deserialize<T>(this XDocument xmlDocument)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using var reader = xmlDocument.CreateReader();
            
            return (T) xmlSerializer.Deserialize(reader);
        }

        public static T Deserialize<T>(this XElement xmlElement)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using var reader = xmlElement.CreateReader();
            
            return (T) xmlSerializer.Deserialize(reader);
        }

        public static XDocument Serialize<T>(this T value)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            var doc = new XDocument();

            using var writer = doc.CreateWriter();
            xmlSerializer.Serialize(writer, value);

            return doc;
        }
    }
}