using System.Collections;
using System.Collections.Generic;
using System.Windows.Data;

namespace ClrVpin.Controls
{
    public class ListCollectionView<T> : ListCollectionView, IEnumerable<T>
    {
        public ListCollectionView(IList<T> list)
            : base((IList) list) { }

        // enumerator required to keep xaml happy.. to make it property aware
        public new IEnumerator<T> GetEnumerator() => new ListCollectionViewEnumerator<T>(base.GetEnumerator());
    }
}
