using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using ClrVpin.Controls;
using ClrVpin.Models.Shared.Game;
using PropertyChanged;
using Utils.Extensions;

namespace ClrVpin.Models.Shared;

[AddINotifyPropertyChangedInterface]
public class Content
{
    // 1 or more content hits (e.g. launch audio, wheel, etc), each of which can contain multiple media file hits (e.g. wrong case, valid, etc)
    // - only the selected content types are added to the collection
    public List<ContentHits> ContentHitsCollection { get; } = new();

    // flattened collection of all media file hits (including valid) across all content types (that checking is enabled)
    public ObservableCollection<Hit> Hits { get; private set; }
    public ListCollectionView HitsView { get; set; }

    // true if game contains any hits types that are not valid
    public bool IsSmelly { get; set; }

    // timestamp of the most recent 'pinball category' (table or backglass) content file
    public DateTime? UpdatedAt { get; private set; }

    public static string GetName(LocalGame localGame, ContentTypeCategoryEnum category) =>
        // determine the correct name - different for media vs pinball
        category == ContentTypeCategoryEnum.Media ? localGame.Game.Description : localGame.Game.Name;

    public void Init(IEnumerable<ContentType> contentTypes)
    {
        // create content hits collection for the specified contentTypes, e.g. the selected contentTypes
        ContentHitsCollection.AddRange(contentTypes.Select(contentType => new ContentHits(contentType)));
    }

    public void Update(Func<IEnumerable<int>> getActiveContentFeatureTypes, Func<IEnumerable<int>> getActiveHitContentTypes)
    {
        // standard properties to avoid cost of recalculating getters during every request (e.g. wpf bindings)
        IsSmelly = ContentHitsCollection.Any(contentHits => contentHits.IsSmelly);

        // timestamp of the most recent 'pinball category' (table or backglass) content file
        var tableAndBackglassContent = ContentHitsCollection
            .Where(contentHits => contentHits.ContentType.Enum.In(ContentTypeEnum.Tables, ContentTypeEnum.Backglasses));
        UpdatedAt = tableAndBackglassContent.Max(contentHits => contentHits.Hits
            .Where(hit => hit.IsPresent)
            .Max(hit => (DateTime?)hit.FileInfo.LastWriteTime));

        Hits = new ObservableCollection<Hit>(ContentHitsCollection.SelectMany(contentHits => contentHits.Hits.ToList()));
        HitsView = new ListCollectionView(Hits)
        {
            // update HitsView based on the updated filtering content type and/or hit type
            // - the getFilteredXxx return their respective enum as an integer via FeatureType.Id
            Filter = hitObject => getActiveContentFeatureTypes().Contains((int)((Hit)hitObject).ContentTypeEnum) &&
                                  getActiveHitContentTypes().Contains((int)((Hit)hitObject).Type)
        };

        var wheelHit = Hits.FirstOrDefault(hit => hit.ContentTypeEnum == ContentTypeEnum.WheelImages);
        WheelImagePath = wheelHit?.IsPresent == true ? wheelHit.Path : null;
    }

    public string WheelImagePath { get; set; }
}