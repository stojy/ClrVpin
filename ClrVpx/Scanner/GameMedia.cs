using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ClrVpx.Models;

namespace ClrVpx.Scanner
{
    public class GameMedia
    {
        public ObservableCollection<Hit> Hits { get; set; } = new ObservableCollection<Hit>();

        // perfect match
        public Hit Valid;

        // use if 'better file', e.g. larger size, more recent, etc.
        public List<Hit> TableNameHits { get; set; } = new List<Hit>();
        public List<Hit> FuzzyHits { get; set; } = new List<Hit>();

        // always use
        public List<Hit> WrongCaseHits { get; set; } = new List<Hit>();

        // always remove - based on file extension priority order
        public List<Hit> DuplicateHits { get; set; } = new List<Hit>();

        public bool IsMissing => Hits.Any(hit => !(hit.Type == HitType.Valid || hit.Type == HitType.WrongCase));
        public bool IsDirty => Hits.Count != 1 || Hits.Any(hit => hit.Type != HitType.Valid);
    }
}