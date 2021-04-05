using System.Linq;
using ClrVpx.Models;

namespace ClrVpx.Scanner
{
    internal class MediaSetup
    {
        public string Folder { get; init; }
        public string[] Extensions { get; init; }
        public MediaHits GetMediaHits(Game game) => game.Media.MediaHitsCollection.First(mediaHits => mediaHits.Type == Folder);

        public string Path => $@"{Settings.SettingsModel.VpxFrontendFolder}\Media\Visual Pinball\{Folder}";
    }
}