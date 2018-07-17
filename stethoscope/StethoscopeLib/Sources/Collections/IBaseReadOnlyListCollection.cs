namespace Stethoscope.Collections
{
    /// <summary>
    /// Represents a read-only collection of items that are optimized for various internal usages within this library.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public interface IBaseReadOnlyListCollection<T> : IBaseListCollection<T>
    {
        /// <summary>
        /// Gets an element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index.</returns>
        T this[int index] { get; }
    }
}
