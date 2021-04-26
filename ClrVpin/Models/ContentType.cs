using System.Linq;

namespace ClrVpin.Models
{
    public class ContentType
    {
        public ContentType(string folder, string[] extensions)
        {
            (Type, Extensions) = (folder, extensions);
            var extensionsOnly = extensions.Select(ext => ext.Substring(2)).ToList();

            ExtensionDetails = $"{extensionsOnly.First()} (or {string.Join(", ", extensionsOnly.Skip(1))})";
        }

        public string ExtensionDetails { get; set; }

        public string Type { get; init; }
        public string[] Extensions { get; init; }
        public ContentHits GetContentHits(Game game) => game.Content.ContentHitsCollection.First(contentHits => contentHits.Type == Type);

        // todo; support table/b2s path
        public string QualifiedFolder => $@"{Model.Config.FrontendFolder}\Media\Visual Pinball\{Type}";
    }
}