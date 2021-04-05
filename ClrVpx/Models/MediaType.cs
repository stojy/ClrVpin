using System.Linq;

namespace ClrVpx.Models
{
    public class MediaType
    {
        public string Folder { get; init; }
        public string[] Extensions { get; init; }
        public MediaHits GetMediaHits(Game game) => game.Media.MediaHitsCollection.First(mediaHits => mediaHits.Type == Folder);

        // todo; support table/b2s path
        public string Path => $@"{Settings.SettingsModel.VpxFrontendFolder}\Media\Visual Pinball\{Folder}";
    }
}