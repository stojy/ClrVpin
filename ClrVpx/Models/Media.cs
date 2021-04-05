using System.Collections.Generic;
using System.Linq;
using Utils;

namespace ClrVpx.Models
{
    public class Media
    {
        public Media()
        {
            SupportedTypes.ForEach(setup => MediaHitsCollection.Add(new MediaHits(setup.Folder)));
        }

        public static MediaType[] SupportedTypes =
        {
            new MediaType {Folder = "Table Audio", Extensions = new[] {"*.mp3", "*.wav"}},
            new MediaType {Folder = "Launch Audio", Extensions = new[] {"*.mp3", "*.wav"}},
            new MediaType {Folder = "Table Videos", Extensions = new[] {"*.f4v", "*.mp4"}},
            new MediaType {Folder = "Backglass Videos", Extensions = new[] {"*.mp4", "*.f4v"}},
            new MediaType {Folder = "Wheel Images", Extensions = new[] {"*.png"}}
            //new MediaType {Folder = "Tables", Extensions = new[] {"*.png"}, GetMediaHits = g => g.WheelImageHits},
            //new MediaType {Folder = "Backglass", Extensions = new[] {"*.png"}, GetMediaHits = g => g.WheelImageHits},
            //new MediaType {Folder = "Point of View", Extensions = new[] {"*.png"}, GetMediaHits = g => g.WheelImageHits},
        };

        public List<MediaHits> MediaHitsCollection { get; set; } = new List<MediaHits>();

        public bool IsSmelly => MediaHitsCollection.Any(media => media.IsSmelly);

        public IEnumerable<Hit> SmellyHits => MediaHitsCollection.SelectMany(media => media.SmellyHits);
    }
}