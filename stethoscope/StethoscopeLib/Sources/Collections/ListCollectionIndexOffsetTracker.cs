namespace Stethoscope.Collections
{
    /// <summary>
    /// Track an index within a <see cref="IBaseListCollection{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of element in the collection.</typeparam>
    public class ListCollectionIndexOffsetTracker<T>
    {
        //TODO: locks, atomics, something to handle concurrency

        /// <summary>
        /// Get or set the original index to track.
        /// </summary>
        public int OriginalIndex { get; set; }

        /// <summary>
        /// Get the current index after events.
        /// </summary>
        public int CurrentIndex { get; private set; }

        /// <summary>
        /// Get the offset between the <see cref="CurrentIndex"/> and <see cref="OriginalIndex"/>.
        /// </summary>
        public int Offset
        {
            get
            {
                return CurrentIndex - OriginalIndex;
            }
        }

        /// <summary>
        /// Reset <see cref="CurrentIndex"/> to <see cref="OriginalIndex"/> so <see cref="Offset"/> is zero.
        /// </summary>
        public void ResetCurrentIndex()
        {
            CurrentIndex = OriginalIndex;
        }

        /// <summary>
        /// Set <see cref="OriginalIndex"/> and reset <see cref="CurrentIndex"/>.
        /// </summary>
        /// <param name="index">The new original index.</param>
        public void SetOriginalIndexAndResetCurrent(int index)
        {
            CurrentIndex = OriginalIndex = index;
        }

        /// <summary>
        /// Handle events that change the index.
        /// </summary>
        /// <param name="sender">The sender of the events.</param>
        /// <param name="e">The event args.</param>
        public void HandleEvent(object sender, ListCollectionEventArgs<T> e)
        {
            if (e != null)
            {
                if (e.Type == ListCollectionEventType.Clear)
                {
                    OriginalIndex = 0;
                    CurrentIndex = 0;
                }
                else if (e.Type == ListCollectionEventType.Add || e.Type == ListCollectionEventType.Insert)
                {
                    if (e.Index <= CurrentIndex)
                    {
                        CurrentIndex++;
                    }
                }
            }
        }
    }
}
