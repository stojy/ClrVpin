using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using Utils;

namespace ClrVpin.Models
{
    public class Content
    {
        public Content()
        {
            SupportedTypes = Model.Config.GetFrontendFolders().Select(folderDetail => new ContentType(folderDetail.Description, folderDetail.Extensions.Split(",").ToArray()));

            SupportedTypes.ForEach(contentType => ContentHitsCollection.Add(new ContentHits(contentType)));
        }

        public List<ContentHits> ContentHitsCollection { get; set; } = new List<ContentHits>();

        public bool IsSmelly { get; set; }
        public ObservableCollection<Hit> SmellyHits { get; set; }
        public ListCollectionView SmellyHitsView { get; set; }

        public void Update(Func<IEnumerable<string>> getFilteredContentTypes, Func<IEnumerable<string>> getFilteredHitTypes)
        {
            // standard properties to avoid cost of recalculating getters during every request (e.g. wpf bindings)
            IsSmelly = ContentHitsCollection.Any(contentHits => contentHits.IsSmelly);
            SmellyHits = new ObservableCollection<Hit>(ContentHitsCollection.SelectMany(contentHits => contentHits.SmellyHits).ToList());
            SmellyHitsView = new ListCollectionView(SmellyHits)
            {
                Filter = hitObject => getFilteredContentTypes().Contains(((Hit) hitObject).ContentType) &&
                                      getFilteredHitTypes().Contains(((Hit) hitObject).Type.GetDescription())
            };
        }

        public const string TableAudio = "Table Audio";
        public const string LaunchAudio = "Launch Audio";
        public const string TableVideos = "Table Videos";
        public const string BackglassVideos = "Backglass Videos";
        public const string WheelImages = "Wheel Images";

        public static string[] Types = {TableAudio, LaunchAudio, TableVideos, BackglassVideos, WheelImages};

        // todo; remove this class and use Config.GetFrontendFolders only
        public static IEnumerable<ContentType> SupportedTypes { get; set; }
    }
}