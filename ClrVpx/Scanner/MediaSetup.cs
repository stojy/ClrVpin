using System;
using System.Collections.ObjectModel;
using ClrVpx.Models;

namespace ClrVpx.Scanner
{
    internal class MediaSetup
    {
        public string MediaFolder { get; init; }
        public string[] Extensions { get; init; }
        public Func<Game, ObservableCollection<Hit>> GetHits { get; init; }

        public string Path => $@"{Settings.SettingsModel.VpxFrontendFolder}\Media\Visual Pinball\{MediaFolder}";
    }
}