using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ClrPin.Models
{
    public class MediaHits
    {
        public MediaHits(MediaType mediaType)
        {
            _mediaType = mediaType;
        }

        private readonly MediaType _mediaType;

        public string Type => _mediaType.Folder;
        public ObservableCollection<Hit> Hits { get; set; } = new ObservableCollection<Hit>();

        public bool IsMissing => Hits.Any(hit => hit.Type == HitType.Missing);
        public bool IsSmelly => SmellyHits.Any();
        public IEnumerable<Hit> SmellyHits => Hits.Where(hit => hit.Type != HitType.Valid);

        public void Add(HitType type, string path)
        {
            // for missing media.. the path is the description, i.e. desirable file name without an extension
            if (type == HitType.Missing)
                path = @$"{_mediaType.QualifiedFolder}\{path}.{_mediaType.ExtensionDetails}";
            Hits.Add(new Hit(Type, path, type));
        }
    }
}