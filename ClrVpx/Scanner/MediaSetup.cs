using ClrVpx.Models;

namespace ClrVpx.Scanner
{
    internal class MediaSetup
    {
        public string Folder { get; init; }
        public string[] Extensions { get; init; }
        public MediaHits GetMediaHits(Game game) => game.Media.MediaHitsCollection[Folder];

        public string Path => $@"{Settings.SettingsModel.VpxFrontendFolder}\Media\Visual Pinball\{Folder}";
    }
}