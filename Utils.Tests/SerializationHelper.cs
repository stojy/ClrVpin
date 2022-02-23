using System.Xml.Linq;
using System.Xml.Serialization;

namespace Utils.Tests;

public static class SerializationHelper
{
    public static T DeserializeFromXDoc<T>(this XDocument source)
    {
        if (source?.Root == null)
            return default;

        using var reader = source.Root.CreateReader();
        return (T)new XmlSerializer(typeof(T)).Deserialize(reader);
    }

    public static XDocument SerializeToXDoc<T>(this T source)
    {
        if (source == null)
            return null;

        var doc = new XDocument();
        using var writer = doc.CreateWriter();
        new XmlSerializer(typeof(T)).Serialize(writer, source);

        return doc;
    }
}