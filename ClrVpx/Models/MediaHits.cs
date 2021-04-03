using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ByteSizeLib;
using PropertyChanged;

namespace ClrVpx.Models
{
    public class MediaHits
    {
        // perfect match
//        public Hit Valid;

        public MediaHits(string expectedName)
        {
            ExpectedName = expectedName;
        }

        public string ExpectedName { get; init; }

        public ObservableCollection<Hit> Hits { get; set; } = new ObservableCollection<Hit>();

        public IEnumerable<string> SmellyResults => IsMissing
            ? new[] {$"Missing: {ExpectedName}"}
            : Hits.Where(hit => hit.Type != HitType.Valid).Select(hit => $"{hit.Type} : {hit.File}");

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


        //// use if 'better file', e.g. larger size, more recent, etc.
        //public List<Hit> TableNameHits { get; set; } = new List<Hit>();
        //public List<Hit> FuzzyHits { get; set; } = new List<Hit>();

        //// always use
        //public List<Hit> WrongCaseHits { get; set; } = new List<Hit>();

        //// always remove - based on file extension priority order
        //public List<Hit> DuplicateHits { get; set; } = new List<Hit>();

        public bool IsMissing => Hits.Any(hit => !(hit.Type == HitType.Valid || hit.Type == HitType.WrongCase));
        //public bool IsSmelly => Hits.Count != 1 || Hits.Any(hit => hit.Type != HitType.Valid);
    }
}