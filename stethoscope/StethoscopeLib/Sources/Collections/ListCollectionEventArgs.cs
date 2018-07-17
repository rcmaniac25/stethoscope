namespace Stethoscope.Collections
{
    /// <summary>
    /// The operations that can be produced by a <see cref="IBaseListCollection{T}"/>.
    /// </summary>
    public enum ListCollectionEventType
    {
        /// <summary>
        /// Add an element.
        /// </summary>
        Add,
        /// <summary>
        /// Insert an element.
        /// </summary>
        Insert,
        /// <summary>
        /// All elements removed from collection.
        /// </summary>
        Clear
    }

    /// <summary>
    /// AN event that has taken place on the <see cref="IBaseListCollection{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of element in the collection.</typeparam>
    public class ListCollectionEventArgs<T> : System.EventArgs
    {
        /// <summary>
        /// Get or set the type of event that occured.
        /// </summary>
        public ListCollectionEventType Type { get; private set; }
        /// <summary>
        /// Get or set the index of the element in the event.
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// Get or set the value of the element in the event.
        /// </summary>
        public T Value { get; private set; }

        private ListCollectionEventArgs()
        {
        }

        /// <summary>
        /// Create an <see cref="ListCollectionEventType.Add"/> event.
        /// </summary>
        /// <param name="index">The index of the value.</param>
        /// <param name="value">The value.</param>
        /// <returns>The created event.</returns>
        public static ListCollectionEventArgs<T> CreateAddEvent(int index, T value)
        {
            return new ListCollectionEventArgs<T>()
            {
                Type = ListCollectionEventType.Add,
                Index = index,
                Value = value
            };
        }

        /// <summary>
        /// Create an <see cref="ListCollectionEventType.Insert"/> event.
        /// </summary>
        /// <param name="index">The index of the value.</param>
        /// <param name="value">The value.</param>
        /// <returns>The created event.</returns>
        public static ListCollectionEventArgs<T> CreateInsertEvent(int index, T value)
        {
            return new ListCollectionEventArgs<T>()
            {
                Type = ListCollectionEventType.Insert,
                Index = index,
                Value = value
            };
        }

        /// <summary>
        /// Create an <see cref="ListCollectionEventType.Clear"/> event.
        /// </summary>
        /// <returns>The created event.</returns>
        public static ListCollectionEventArgs<T> CreateClearEvent()
        {
            return new ListCollectionEventArgs<T>()
            {
                Type = ListCollectionEventType.Clear
            };
        }
    }
}
