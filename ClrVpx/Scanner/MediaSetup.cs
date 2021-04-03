using ClrVpx.Models;

namespace ClrVpx.Scanner
{
    internal class MediaSetup
    {
        public string Folder { get; init; }
        public string[] Extensions { get; init; }
        public MediaHits GetHits(Game game) => game.Media.MediaHits[Folder];

        public string Path => $@"{Settings.SettingsModel.VpxFrontendFolder}\Media\Visual Pinball\{Folder}";
    }
}