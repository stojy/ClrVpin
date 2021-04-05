using System.Collections.Generic;
using System.Linq;
using Utils;

namespace ClrVpx.Models
{
    public class Media
    {
        public Media()
        {
            SupportedTypes.ForEach(mediaType => MediaHitsCollection.Add(new MediaHits(mediaType)));
        }

        public static MediaType[] SupportedTypes =
        {
            new MediaType("Table Audio", new[] {"*.mp3", "*.wav"}),
            new MediaType("Launch Audio", new[] {"*.mp3", "*.wav"}),
            new MediaType("Table Videos", new[] {"*.f4v", "*.mp4"}),
            new MediaType("Backglass Videos", new[] {"*.mp4", "*.f4v"}),
            new MediaType("Wheel Images", new[] {"*.png", "*.jpg"})
            //new MediaType {Folder = "Tables", Extensions = new[] {"*.png"}, GetMediaHits = g => g.WheelImageHits},
            //new MediaType {Folder = "Backglass", Extensions = new[] {"*.png"}, GetMediaHits = g => g.WheelImageHits},
            //new MediaType {Folder = "Point of View", Extensions = new[] {"*.png"}, GetMediaHits = g => g.WheelImageHits},
        };

        public List<MediaHits> MediaHitsCollection { get; set; } = new List<MediaHits>();

        public bool IsSmelly => MediaHitsCollection.Any(media => media.IsSmelly);

        public IEnumerable<Hit> SmellyHits => MediaHitsCollection.SelectMany(media => media.SmellyHits);
    }
}