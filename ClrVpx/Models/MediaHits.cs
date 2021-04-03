using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ByteSizeLib;
using Utils;

namespace ClrVpx.Models
{
    public class MediaHits
    {
        private readonly string _expectedName;

        public MediaHits(string expectedName)
        {
            _expectedName = expectedName;
        }

        public ObservableCollection<Hit> Hits { get; set; } = new ObservableCollection<Hit>();

        public bool IsMissing => !Hits.Any(hit => hit.Type == HitType.Valid || hit.Type == HitType.WrongCase);

        public IEnumerable<string> GetSmellyResults(string mediaType)
        {
            return IsMissing
                ? new[] {$"Missing file: {_expectedName}"}
                : Hits.Where(hit => hit.Type != HitType.Valid).Select(hit => $"{mediaType} - {hit.Type.GetDescription()} : {hit.File}");
        }

        public void Add(HitType type, string path)
        {
            var hit = new Hit
            {
                Path = path,
                File = Path.GetFileName(path),
                Size = ByteSize.FromBytes(new FileInfo(path).Length).ToString("#"),
                Type = type
            };
            Hits.Add(hit);
        }

        public bool IsSmelly => Hits.Count != 1 || Hits.Any(hit => hit.Type != HitType.Valid);
    }
}