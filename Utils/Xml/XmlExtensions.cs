using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Utils.Extensions;

namespace Utils.Xml;

public static class XmlExtensions
{
    public static T Deserialize<T>(this XDocument document)
    {
        var xmlSerializer = new XmlSerializer(typeof(T));
        using var reader = document.CreateReader();

        return (T)xmlSerializer.Deserialize(reader);
    }

    public static T Deserialize<T>(this XElement element)
    {
        var xmlSerializer = new XmlSerializer(typeof(T));
        using var reader = element.CreateReader();

        return (T)xmlSerializer.Deserialize(reader);
    }

    public static XDocument SerializeToXDocument<T>(this T value)
    {
        // create writer associated with a new XDocument
        var document = new XDocument();
        using var writer = document.CreateWriter();

        // serialize object using the writer into the new XDocument
        var xmlSerializer = new XmlSerializer(typeof(T));
        xmlSerializer.Serialize(writer, value);

        return document;
    }

    public static void SerializeToFile(this XDocument document, string file)
    {
        // standard XmlWriter writer behavior..
        // - elements with no content (aka empty) - written as self closing tag, e.g. <blah />
        // - elements with blank content - written as non-self closing tag, e.g. <blah></blah>
        using var writer = XmlWriter.Create(file, new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "\t",
            Encoding = Encoding.GetEncoding("Windows-1252"),
            DoNotEscapeUriAttributes = true
        });

        document.Save(writer);
    }

    public static void SerializeToFile(this XDocument document, XmlTextWriter writer)
    {
        writer.IndentChar = '\t';
        writer.Indentation = 1;
        writer.Formatting = Formatting.Indented;

        document.Save(writer);
    }

    public static XDocument Cleanse(this XDocument document)
    {
        // remove the namespace attributes
        document.Root!.RemoveAttributes();

        // replace empty tags with blank content.. so that the tags are written as non-self closing
        document.AssignEmptyElements();

        return document;
    }

    public static XmlWriter CreateNonSelfClosingWriter(string file) => new NonSelfClosingXmlTextWriter(file, Encoding.GetEncoding("Windows-1252"));

    private static void AssignEmptyElements(this XContainer container)
    {
        AssignEmptyElements(container.FirstNode);
    }

    private static void AssignEmptyElements(this XNode node)
    {
        // recursively assign empty elements with an empty string so the default XmlWriter outputs them as non-self closing tags
        if (node is XElement e)
        {
            e.Nodes().ForEach(AssignEmptyElements);

            if (e.IsEmpty)
                e.Value = string.Empty;
        }
    }
}