using System.Collections.ObjectModel;
using ClrVpx.Models;

namespace ClrVpx.Scanner
{
    internal class MediaSetup
    {
        public string Folder { get; init; }
        public string[] Extensions { get; init; }
        public ObservableCollection<Hit> GetHits(Game game) => game.Media[Folder].Hits;

        public string Path => $@"{Settings.SettingsModel.VpxFrontendFolder}\Media\Visual Pinball\{Folder}";
    }
}