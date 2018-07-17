using System;
using System.Collections;
using System.Collections.Generic;

namespace Stethoscope.Collections
{
    /// <summary>
    /// Represents a collection of items that are optimized for various internal usages within this library.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public interface IBaseListCollection<T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// Event handler for when things happen in the collection.
        /// </summary>
        event EventHandler<ListCollectionEventArgs<T>> CollectionChangedEvent;

        /// <summary>
        /// Get the number of elements contained in the <see cref="IBaseListCollection{T}"/>.
        /// </summary>
        int Count { get; }
        /// <summary>
        /// Gets a value indicating whether the <see cref="IBaseListCollection{T}"/> is read-only.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets an element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index.</returns>
        T GetAt(int index);

        /// <summary>
        /// Searches a range of elements in the sorted <see cref="IBaseListCollection{T}"/> for an element using the specified comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to search.</param>
        /// <param name="count">The length of the range to search.</param>
        /// <param name="item">The object to locate. The value can be <b>null</b> for reference types.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements.</param>
        /// <returns>The zero-based index of item in the sorted <see cref="IBaseListCollection{T}"/>, if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than <paramref name="item"/> or, if there is no larger element, the bitwise complement of <see cref="Count"/>.</returns>
        int BinarySearch(int index, int count, T item, IComparer<T> comparer);
    }
}
