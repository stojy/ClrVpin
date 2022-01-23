using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Data;

namespace ClrVpin.Controls
{
    public sealed class ListCollectionView<T> : ListCollectionView, IEnumerable<T>
    {
        //todo; add ctor that takes delegate to update on refresh
        public ListCollectionView(IList<T> list)
            : base((IList)list)
        {
            // remove selected item to avoid defaulting to the first item
            MoveCurrentTo(null);
        }

        // typed filter.. assign to the base untyped filter so that it will be invoked as required
        public new Predicate<T> Filter
        {
            get => base.Filter as Predicate<T>;

            // checking x != null by confirming type is T
            set => base.Filter = x => x is T obj && value(obj);
        }

        // enumerator required to keep xaml happy.. to make it property aware
        public new IEnumerator<T> GetEnumerator() => new ListCollectionViewEnumerator<T>(base.GetEnumerator());
    }
}