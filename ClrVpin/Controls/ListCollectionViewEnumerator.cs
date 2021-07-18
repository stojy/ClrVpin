using System.Collections;
using System.Collections.Generic;

namespace ClrVpin.Controls
{
    // generic LCV enumerator
    public class ListCollectionViewEnumerator<T> : IEnumerator<T>
    {
        public ListCollectionViewEnumerator(IEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public void Dispose() { }
        public bool MoveNext() => _enumerator.MoveNext();
        public void Reset() => _enumerator.Reset();

        public T Current => (T) _enumerator.Current;
        object IEnumerator.Current => Current;

        private readonly IEnumerator _enumerator;
    }
}