using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ClrVpin.Models
{
    public class ContentHits
    {
        public ContentHits(ContentType contentType)
        {
            _contentType = contentType;
        }

        private readonly ContentType _contentType;

        public string Type => _contentType.Type;
        public ObservableCollection<Hit> Hits { get; set; } = new ObservableCollection<Hit>();

        public bool IsMissing => Hits.Any(hit => hit.Type == HitType.Missing);
        public bool IsSmelly => SmellyHits.Any();
        public IEnumerable<Hit> SmellyHits => Hits.Where(hit => hit.Type != HitType.Valid);

        public void Add(HitType hitType, string path)
        {
            // for missing content.. the path is the description, i.e. desirable file name without an extension
            if (hitType == HitType.Missing)
                path = @$"{_contentType.QualifiedFolder}\{path}.{_contentType.ExtensionDetails}";

            // only add hit type for valid hits OR if it has been configured to be checked
            if (hitType == HitType.Valid || Config.CheckHitTypes.Contains(hitType))
                Hits.Add(new Hit(Type, path, hitType));
        }
    }
}