using System.Linq;

namespace ClrPin.Models
{
    public class MediaType
    {
        public MediaType(string folder, string[] extensions)
        {
            (Folder, Extensions) = (folder, extensions);
            var extensionsOnly = extensions.Select(ext => ext.Substring(2)).ToList();

            ExtensionDetails = $"{extensionsOnly.First()} (or {string.Join(", ", extensionsOnly.Skip(1))})";
        }

        public string ExtensionDetails { get; set; }

        public string Folder { get; init; }
        public string[] Extensions { get; init; }
        public MediaHits GetMediaHits(Game game) => game.Media.MediaHitsCollection.First(mediaHits => mediaHits.Type == Folder);

        // todo; support table/b2s path
        public string QualifiedFolder => $@"{Settings.SettingsModel.VpxFrontendFolder}\Media\Visual Pinball\{Folder}";
    }
}