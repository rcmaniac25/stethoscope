using System;
using System.Threading;

namespace Stethoscope.Collections
{
    /// <summary>
    /// Track an index within a <see cref="IBaseListCollection{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of element in the collection.</typeparam>
    public class ListCollectionIndexOffsetTracker<T>
    {
        // Would prefer to use atomics instead of locks...

        private readonly object locker = new object();
        private bool isLockContextInUse = false;

        /// <summary>
        /// Get or set the original index to track.
        /// </summary>
        /// <seealso cref="ApplyContextLock(Action{ListCollectionIndexOffsetTracker{T}})"/>
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
                lock (locker)
                {
                    return CurrentIndex - OriginalIndex;
                }
            }
        }

        /// <summary>
        /// Reset <see cref="CurrentIndex"/> to <see cref="OriginalIndex"/> so <see cref="Offset"/> is zero.
        /// </summary>
        /// <seealso cref="ApplyContextLock(Action{ListCollectionIndexOffsetTracker{T}})"/>
        public void ResetCurrentIndex()
        {
            lock (locker)
            {
                CurrentIndex = OriginalIndex;
            }
        }

        /// <summary>
        /// Set <see cref="OriginalIndex"/> and reset <see cref="CurrentIndex"/>.
        /// </summary>
        /// <param name="index">The new original index.</param>
        /// <seealso cref="ApplyContextLock(Action{ListCollectionIndexOffsetTracker{T}})"/>
        public void SetOriginalIndexAndResetCurrent(int index)
        {
            lock (locker)
            {
                CurrentIndex = OriginalIndex = index;
            }
        }

        /// <summary>
        /// Do an action involving the tracker without any events or outside actions influencing it.
        /// </summary>
        /// <param name="lockedContext">The action to take place. Should not be long running.</param>
        /// <exception cref="InvalidOperationException">If <see cref="ApplyContextLock(Action{ListCollectionIndexOffsetTracker{T}})"/> is recursivly invoked.</exception>
        public void ApplyContextLock(Action<ListCollectionIndexOffsetTracker<T>> lockedContext)
        {
            if (lockedContext == null)
            {
                throw new ArgumentNullException(nameof(lockedContext));
            }

            // Logic is to prevent throwing an exception inside a "lock" to maintain state, AKA, the lockDepth value (https://stackoverflow.com/a/15860936/492347)
            // Reason for caring about lock depth is it would eventually cause holding the lock for longer then we want, and it implies complexity that would better require a workaround on the dev's part then the framework
            Exception lateException = null;
            lock (locker)
            {
                if (isLockContextInUse)
                {
                    lateException = new InvalidOperationException("Lock has already been invoked and cannot be invoked additional times");
                }
                else
                {
                    // "Oh no, what if multiple threads try to use this at the same time? This state will be wrong!" Nope. Other threads will hit the outer lock and, well, be locked.
                    // Meanwhile the thread that holds the lock will have access to the state, and the exception handling we do will ensure the state gets reset before the final lock gets released.
                    isLockContextInUse = true;
                    try
                    {
                        lockedContext(this);
                    }
                    catch (Exception e)
                    {
                        lateException = e;
                    }
                    isLockContextInUse = false;
                }
            }
            if (lateException != null)
            {
                throw lateException;
            }
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
                    lock (locker)
                    {
                        OriginalIndex = 0;
                        CurrentIndex = 0;
                    }
                }
                else if (e.Type == ListCollectionEventType.Insert)
                {
                    lock (locker)
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
}
