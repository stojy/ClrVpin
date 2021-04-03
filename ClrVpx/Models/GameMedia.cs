using System.Collections.Generic;
using System.Linq;

namespace ClrVpx.Models
{
    public class GameMedia
    {
        private Game _game;

        public void Init(Game game)
        {
            _game = game;

            MediaHits.Add(Scanner.Scanner.MediaLaunchAudio, new MediaHits(_game.Description));
            MediaHits.Add(Scanner.Scanner.MediaTableAudio, new MediaHits(_game.Description));
            MediaHits.Add(Scanner.Scanner.MediaTableVideos, new MediaHits(_game.Description));
            MediaHits.Add(Scanner.Scanner.MediaBackglassVideos, new MediaHits(_game.Description));
            MediaHits.Add(Scanner.Scanner.MediaWheelImages, new MediaHits(_game.Description));
        }

        public Dictionary<string, MediaHits> MediaHits { get; set; } = new Dictionary<string, MediaHits>();

        public bool IsSmelly => MediaHits.Any(media => media.Value.SmellyResults.Any());
        public IEnumerable<string> SmellyResults => MediaHits.SelectMany(media => media.Value.SmellyResults);
    }
}