using System;
using System.Collections.Generic;

namespace Stethoscope.Collections
{
    /// <summary>
    /// Extensions for the classes within the Stethoscope.Collections namespace.
    /// </summary>
    public static class CollectionsExtensions
    {
        /// <summary>
        /// Searches the entire sorted <see cref="IBaseListCollection{T}"/> for an element using the default comparer and returns the zero-based index of the element.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="collection">The collection to search.</param>
        /// <param name="item">The object to locate. The value can be <b>null</b> for reference types.</param>
        /// <returns>The zero-based index of item in the sorted <see cref="IBaseListCollection{T}"/>, if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than <paramref name="item"/> or, if there is no larger element, the bitwise complement of <see cref="Count"/>.</returns>
        public static int BinarySearch<T>(this IBaseListCollection<T> collection, T item)
        {
            return collection.BinarySearch(0, collection.Count, item, null);
        }

        /// <summary>
        /// Searches the entire sorted <see cref="IBaseListCollection{T}"/> for an element using the specified comparer and returns the zero-based index of the element.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="collection">The collection to search.</param>
        /// <param name="item">The object to locate. The value can be <b>null</b> for reference types.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements.</param>
        /// <returns>The zero-based index of item in the sorted <see cref="IBaseListCollection{T}"/>, if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than <paramref name="item"/> or, if there is no larger element, the bitwise complement of <see cref="Count"/>.</returns>
        public static int BinarySearch<T>(this IBaseListCollection<T> collection, T item, IComparer<T> comparer)
        {
            return collection.BinarySearch(0, collection.Count, item, comparer);
        }

        /// <summary>
        /// Get a read-only list as a <see cref="IBaseReadOnlyListCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="list">The read-only list of data.</param>
        /// <returns>The <see cref="IBaseReadOnlyListCollection{T}"/> representing <paramref name="list"/>.</returns>
        public static IBaseReadOnlyListCollection<T> AsListCollection<T>(this IReadOnlyList<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            return new ReadOnlyListCollection<T>(list);
        }

        /// <summary>
        /// Get a read/write list as a <see cref="IBaseReadWriteListCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="list">The read/write list of data.</param>
        /// <returns>The <see cref="IBaseReadWriteListCollection{T}"/> representing <paramref name="list"/>.</returns>
        public static IBaseReadWriteListCollection<T> AsListCollection<T>(this IList<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            return new ReadWriteListCollection<T>(list);
        }
    }
}
