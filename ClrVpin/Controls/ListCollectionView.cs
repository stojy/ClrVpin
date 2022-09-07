using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Threading;

namespace ClrVpin.Controls
{
    public sealed class ListCollectionView<T> : ListCollectionView, IEnumerable<T>
    {
        // interesting notes..
        // - Refresh must happen on the UI thread.. potentially an async thread of lower priority
        // - unlike the source collection itself which can be updated form 

        //todo; add ctor that takes delegate to update on refresh
        public ListCollectionView(IList<T> list)
            : base((IList)list)
        {
            // remove selected item to avoid defaulting to the first item
            MoveCurrentTo(null);
            IsLiveFiltering = false;
            IsLiveGrouping = false;
            IsLiveSorting = false;
        }

        // typed filter.. assign to the base untyped filter so that it will be invoked as required
        public new Predicate<T> Filter
        {
            get => base.Filter as Predicate<T>;

            // checking x != null by confirming type is T
            set => base.Filter = x => x is T obj && value(obj);
        }

        public override void Refresh()
        {
            // must be called on the UI thread
            // - invoked asynchronously AND with a lower priority to minimize disruption to the UI responsiveness (e.g. user typing)
            // - note.. the underlying source collection can be updated from any thread so long as there is thread safety
            //   https: //stackoverflow.com/a/40375940/227110

            if (!CheckAccess())
                throw new Exception("LCV.Refresh invoked on the incorrect thread.. must be on the UI thread!");
            
            Dispatcher.CurrentDispatcher.InvokeAsync(() => base.Refresh(), DispatcherPriority.ApplicationIdle);
        }

        // enumerator required to keep xaml happy.. to make it property aware
        public new IEnumerator<T> GetEnumerator() => new ListCollectionViewEnumerator<T>(base.GetEnumerator());
    }
}