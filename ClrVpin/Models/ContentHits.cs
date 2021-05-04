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

        public string Type => _contentType.Type;
        public ObservableCollection<Hit> Hits { get; set; } = new ObservableCollection<Hit>();

        public bool IsMissing => Hits.Any(hit => hit.Type == HitTypeEnum.Missing);
        public bool IsSmelly => SmellyHits.Any();
        public IEnumerable<Hit> SmellyHits => Hits.Where(hit => hit.Type != HitTypeEnum.Valid);

        public void Add(HitTypeEnum hitType, string path)
        {
            // for missing content.. the path is the description, i.e. desirable file name without an extension
            if (hitType == HitTypeEnum.Missing)
                path = @$"{_contentType.Folder}\{path}.{string.Join(", ", _contentType.Extensions)}";

            // only add hit type for valid hits OR if it has been configured to be checked
            if (hitType == HitTypeEnum.Valid || Model.Config.CheckHitTypes.Contains(hitType))
                Hits.Add(new Hit(Type, path, hitType));
        }

        private readonly ContentType _contentType;
    }
}