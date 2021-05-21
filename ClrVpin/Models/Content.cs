using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using Utils;

namespace ClrVpin.Models
{
    // Content contains 1 or more content hits (e.g. launch audio, wheel, etc), each of which can contain multiple media file hits (e.g. wrong case, valid, etc)
    public class Content
    {
        public Content()
        {
            // create content hits collection.. 1 entry for every content type
            var contentTypes = Model.Config.GetFrontendFolders().Where(x => !x.IsDatabase).ToList();
            contentTypes.ForEach(contentType => ContentHitsCollection.Add(new ContentHits(contentType)));
        }

        // 1 or more content hits (e.g. launch audio, wheel, etc), each of which can contain multiple media file hits (e.g. wrong case, valid, etc)
        public List<ContentHits> ContentHitsCollection { get; set; } = new List<ContentHits>();

        // flattened collection of all smelly media file hits across all content types
        public ObservableCollection<Hit> SmellyHits { get; set; }
        public ListCollectionView SmellyHitsView { get; set; }
        
        // flattened collection of all media file hits (including smelly) across all content types (that checking is enabled)
        public ObservableCollection<Hit> Hits { get; set; }

        // true if game contains any smelly hits
        public bool IsSmelly { get; set; }

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

            Hits = new ObservableCollection<Hit>(ContentHitsCollection.SelectMany(contentHits => contentHits.Hits.ToList()));
        }
    }
}