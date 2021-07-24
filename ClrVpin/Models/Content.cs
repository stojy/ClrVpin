using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

namespace ClrVpin.Models
{
    // Content contains 1 or more content hits (e.g. launch audio, wheel, etc), each of which can contain multiple media file hits (e.g. wrong case, valid, etc)
    public class Content
    {
        public Content()
        {
            // create content hits collection
            var contentTypes = Model.Settings.GetSelectedCheckContentTypes().ToList();
            contentTypes.ForEach(contentType => ContentHitsCollection.Add(new ContentHits(contentType)));
        }

        // 1 or more content hits (e.g. launch audio, wheel, etc), each of which can contain multiple media file hits (e.g. wrong case, valid, etc)
        public List<ContentHits> ContentHitsCollection { get; set; } = new List<ContentHits>();
       
        // flattened collection of all media file hits (including valid) across all content types (that checking is enabled)
        public ObservableCollection<Hit> Hits { get; set; }
        public ListCollectionView HitsView { get; set; }

        // true if game contains any hits types that are not valid
        public bool IsSmelly { get; set; }

        public void Update(Func<IEnumerable<int>> getFilteredContentTypes, Func<IEnumerable<int>> getFilteredHitTypes)
        {
            // standard properties to avoid cost of recalculating getters during every request (e.g. wpf bindings)
            IsSmelly = ContentHitsCollection.Any(contentHits => contentHits.IsSmelly);

            Hits = new ObservableCollection<Hit>(ContentHitsCollection.SelectMany(contentHits => contentHits.Hits.ToList()));
            HitsView = new ListCollectionView(Hits)
            {
                // update HitsView based on the updated filtering content type and/or hit type
                // - the getFilteredXxx return their respective enum as an integer via FeatureType.Id
                Filter = hitObject => getFilteredContentTypes().Contains((int) ((Hit) hitObject).ContentTypeEnum) &&
                                      getFilteredHitTypes().Contains((int) ((Hit) hitObject).Type)
            };
        }
    }
}