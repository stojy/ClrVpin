using System.Collections.ObjectModel;
using System.Linq;
using ClrVpx.Models;

namespace ClrVpx.Scanner
{
    public class GameMedia
    {
        public ObservableCollection<Hit> Hits { get; set; } = new ObservableCollection<Hit>();

        public bool IsDirty => Hits.Count != 1 || Hits.Any(m => m.Score != 100);
    }
}