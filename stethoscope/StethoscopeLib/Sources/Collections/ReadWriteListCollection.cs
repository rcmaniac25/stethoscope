using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Stethoscope.Collections
{
    internal class ReadWriteListCollection<T> : IBaseReadWriteListCollection<T>
    {
        private IList<T> list;

        public ReadWriteListCollection(IList<T> list)
        {
            this.list = list;
        }

        public T this[int index] { get => list[index]; set => list[index] = value; }
        public int Count => list.Count;
        public bool IsReadOnly => false;

        public event EventHandler<ListCollectionEventArgs<T>> CollectionChangedEvent;

        public void Add(T item)
        {
            list.Add(item);
            OnRaiseCollectionChangedEvent(ListCollectionEventArgs<T>.CreateAddEvent(list.Count - 1, item));
        }

        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            if (this.list is List<T> list)
            {
                return list.BinarySearch(index, count, item, comparer);
            }
            //XXX Heavy... (converting to an array each time)
            return Array.BinarySearch(this.list.ToArray(), index, count, item, comparer);
        }

        public void Clear()
        {
            list.Clear();
            OnRaiseCollectionChangedEvent(ListCollectionEventArgs<T>.CreateClearEvent());
        }

        public T GetAt(int index)
        {
            return list[index];
        }

        public void Insert(int index, T item)
        {
            list.Insert(index, item);
            OnRaiseCollectionChangedEvent(ListCollectionEventArgs<T>.CreateInsertEvent(index, item));
        }

        protected virtual void OnRaiseCollectionChangedEvent(ListCollectionEventArgs<T> e)
        {
            CollectionChangedEvent?.Invoke(this, e);
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
