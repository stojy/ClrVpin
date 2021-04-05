using System.Collections.Generic;
using System.Linq;

namespace ClrVpx.Models
{
    public class GameMedia
    {
        public GameMedia()
        {
            // todo; change to list
            MediaHitsCollection.Add(Scanner.Scanner.MediaLaunchAudio, new MediaHits(Scanner.Scanner.MediaLaunchAudio));
            MediaHitsCollection.Add(Scanner.Scanner.MediaTableAudio, new MediaHits(Scanner.Scanner.MediaTableAudio));
            MediaHitsCollection.Add(Scanner.Scanner.MediaTableVideos, new MediaHits(Scanner.Scanner.MediaTableVideos));
            MediaHitsCollection.Add(Scanner.Scanner.MediaBackglassVideos, new MediaHits(Scanner.Scanner.MediaBackglassVideos));
            MediaHitsCollection.Add(Scanner.Scanner.MediaWheelImages, new MediaHits(Scanner.Scanner.MediaWheelImages));
        }

        public Dictionary<string, MediaHits> MediaHitsCollection { get; set; } = new Dictionary<string, MediaHits>();

        public bool IsSmelly => MediaHitsCollection.Any(media => media.Value.IsSmelly);

        public IEnumerable<Hit> SmellyHits => MediaHitsCollection.SelectMany(media => media.Value.SmellyHits);
    }
}