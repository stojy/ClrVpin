using System.Text;
using System.Xml;

namespace Utils.Xml;

public class NonSelfClosingXmlTextWriter : XmlTextWriter
{
    public NonSelfClosingXmlTextWriter(string fileName, Encoding encoding)
        : base(fileName, encoding)
    {
    }

    public override void WriteEndElement()
    {
        // temporarily disable formatting so the end tag is written on the same line.. which isn't the default behavior unfortunately for an empty element
        var formatting = Formatting;
        Formatting = Formatting.None;
        
        // write end tag WITHOUT self closing , e.g. <blah></blah> instead of <blah/>
        base.WriteFullEndElement();
        Formatting = formatting;
    }
}