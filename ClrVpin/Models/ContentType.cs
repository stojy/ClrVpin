using System.Linq;

namespace ClrVpin.Models
{
    public class ContentType
    {
        public ContentType(string description, string qualifiedFolder, string[] extensions)
        {
            (Type, QualifiedFolder, Extensions) = (description, qualifiedFolder, extensions);
            var extensionsOnly = extensions.Select(ext => ext.Substring(2)).ToList();

            ExtensionDetails = $"{extensionsOnly.First()} (or {string.Join(", ", extensionsOnly.Skip(1))})";
        }

        public string Type { get; init; }
        public string QualifiedFolder { get; init; }
        public string[] Extensions { get; init; }

        public string ExtensionDetails { get; set; }

        // todo; support table/b2s path
        public ContentHits GetContentHits(Game game) => game.Content.ContentHitsCollection.First(contentHits => contentHits.Type == Type);
    }
}