namespace ClrVpin.Models
{
    public class ContentType
    {
        public ContentType(string description, string qualifiedFolder, string[] extensions)
        {
            (Type, QualifiedFolder, Extensions) = (description, qualifiedFolder, extensions);
        }

        public string Type { get; init; }
        public string QualifiedFolder { get; init; }
        public string[] Extensions { get; init; }
    }
}