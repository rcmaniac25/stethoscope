using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Stethoscope.Collections
{
    internal class ReadOnlyListCollection<T> : IBaseReadOnlyListCollection<T>
    {
        private IReadOnlyList<T> list;

        public ReadOnlyListCollection(IReadOnlyList<T> list)
        {
            this.list = list;
        }

        public T this[int index] => list[index];
        public int Count => list.Count;
        public bool IsReadOnly => true;

        // Unused
        public event EventHandler<ListCollectionEventArgs<T>> CollectionChangedEvent;

        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            if (this.list is List<T> list)
            {
                return list.BinarySearch(index, count, item, comparer);
            }
            //XXX Heavy... (converting to an array each time)
            return Array.BinarySearch(this.list.ToArray(), index, count, item, comparer);
        }

        public T GetAt(int index)
        {
            return list[index];
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }
}
