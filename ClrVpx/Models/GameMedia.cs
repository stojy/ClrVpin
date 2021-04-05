using System.Collections.Generic;
using System.Linq;

namespace ClrVpx.Models
{
    public class GameMedia
    {
        public GameMedia()
        {
            MediaHitsCollection.Add(new MediaHits(Scanner.Scanner.MediaLaunchAudio));
            MediaHitsCollection.Add(new MediaHits(Scanner.Scanner.MediaTableAudio));
            MediaHitsCollection.Add(new MediaHits(Scanner.Scanner.MediaTableVideos));
            MediaHitsCollection.Add(new MediaHits(Scanner.Scanner.MediaBackglassVideos));
            MediaHitsCollection.Add(new MediaHits(Scanner.Scanner.MediaWheelImages));
        }

        public List<MediaHits> MediaHitsCollection { get; set; } = new List<MediaHits>();

        public bool IsSmelly => MediaHitsCollection.Any(media => media.IsSmelly);

        public IEnumerable<Hit> SmellyHits => MediaHitsCollection.SelectMany(media => media.SmellyHits);
    }
}