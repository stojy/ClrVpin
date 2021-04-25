using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Utils
{
    // keep StringCollection in sync with collection..
    // - create observable collection
    // - if observable changes, then update the original StringCollection
    public class ObservableStringCollection<T>
    {
        public ObservableStringCollection(StringCollection stringCollection)
        {
            Observable = new ObservableCollection<T>(stringCollection.Cast<T>());

            Observable.CollectionChanged += (_, _) =>
            {
                // since performance isn't a concern, keep things simple and verbatim overwrite the settings.. i.e. ignore event type add/remove/move/et
                stringCollection.Clear();
                stringCollection.AddRange(Observable.Select(x => x.ToString()).ToArray());
            };
        }

        public ObservableCollection<T> Observable { get; set; }
    }
}