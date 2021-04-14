using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using Utils;

namespace ClrVpin.Models
{
    // todo; perhaps rename to content?  e.g. as not to exclude b2s and vpx
    public class Media
    {
        public Media()
        {
            SupportedTypes.ForEach(mediaType => MediaHitsCollection.Add(new MediaHits(mediaType)));
        }

        public const string TableAudio = "Table Audio";
        public const string LaunchAudio = "Launch Audio";
        public const string TableVideos = "Table Videos";
        public const string BackglassVideos = "Backglass Videos";
        public const string WheelImages = "Wheel Images";

        public static string[] Types = {TableAudio, LaunchAudio, TableVideos, BackglassVideos, WheelImages};

        public static MediaType[] SupportedTypes =
        {
            new MediaType(TableAudio, new[] {"*.mp3", "*.wav"}),
            new MediaType(LaunchAudio, new[] {"*.mp3", "*.wav"}),
            new MediaType(TableVideos, new[] {"*.f4v", "*.mp4"}),
            new MediaType(BackglassVideos, new[] {"*.mp4", "*.f4v"}),
            new MediaType(WheelImages, new[] {"*.png", "*.jpg"})
            //new MediaType {Folder = "Tables", Extensions = new[] {"*.png"}, GetMediaHits = g => g.WheelImageHits},
            //new MediaType {Folder = "Backglass", Extensions = new[] {"*.png"}, GetMediaHits = g => g.WheelImageHits},
            //new MediaType {Folder = "Point of View", Extensions = new[] {"*.png"}, GetMediaHits = g => g.WheelImageHits},
        };

        public List<MediaHits> MediaHitsCollection { get; set; } = new List<MediaHits>();

        public bool IsSmelly { get; set; }
        public ObservableCollection<Hit> SmellyHits { get; set; }
        public ListCollectionView SmellyHitsView { get; set; }

        public void Update(Func<IEnumerable<string>> getFilteredMedia, Func<IEnumerable<HitType>> getFilteredHitTypes)
        {
            // standard properties to avoid cost of recalculating getters during every request (e.g. wpf bindings)
            IsSmelly = MediaHitsCollection.Any(media => media.IsSmelly);
            SmellyHits = new ObservableCollection<Hit>(MediaHitsCollection.SelectMany(media => media.SmellyHits).ToList());
            SmellyHitsView = new ListCollectionView(SmellyHits)
            {
                Filter = hitObject => getFilteredMedia().Contains(((Hit) hitObject).MediaType) && getFilteredHitTypes().Contains(((Hit) hitObject).Type)
            };
        }
    }
}