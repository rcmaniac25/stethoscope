namespace Stethoscope.Collections
{
    /// <summary>
    /// Represents a read/write collection of items that are optimized for various internal usages within this library.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public interface IBaseReadWriteListCollection<T> : IBaseListCollection<T>
    {
        /// <summary>
        /// Gets or sets an element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        T this[int index] { get; set; }

        /// <summary>
        /// Adds an item to the <see cref="IBaseReadWriteListCollection{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="IBaseReadWriteListCollection{T}"/>.</param>
        void Add(T item);
        /// <summary>
        /// Inserts an item to the <see cref="IBaseReadWriteListCollection{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="IBaseReadWriteListCollection{T}"/>.</param>
        void Insert(int index, T item);
        /// <summary>
        /// Removes all items from the <see cref="IBaseReadWriteListCollection{T}"/>.
        /// </summary>
        void Clear();
    }
}
