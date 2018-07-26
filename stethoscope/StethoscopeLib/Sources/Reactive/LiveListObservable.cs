using Stethoscope.Collections;

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;

namespace Stethoscope.Reactive
{
    #region LiveListObservable

    internal class LiveListObservable<T> : ScheduledObservable<T, (IBaseListCollection<T> list, ListCollectionIndexOffsetTracker<T> tracker)>
    {
        private const int STARTING_INDEX = 0;

        private static ListCollectionIndexOffsetTracker<T> CreateTracker(IBaseListCollection<T> list, int startingIndex)
        {
            var tracker = new ListCollectionIndexOffsetTracker<T>()
            {
                OriginalIndex = startingIndex
            };
            list.CollectionChangedEvent += tracker.HandleEvent;
            return tracker;
        }
        
        public LiveListObservable(IBaseListCollection<T> list, IScheduler scheduler, int startingIndex = STARTING_INDEX) : base(ObservableType.LiveUpdating, scheduler, () => (list, CreateTracker(list, startingIndex)))
        {
            SupportsLongRunning = true;
        }

        protected override void IndividualExecution((IBaseListCollection<T> list, ListCollectionIndexOffsetTracker<T> tracker) state, IObserver<T> observer, Action<(IBaseListCollection<T> list, ListCollectionIndexOffsetTracker<T> tracker)> continueExecution)
        {
            var tracker = state.tracker;
            try
            {
                if (tracker.CurrentIndex >= state.list.Count)
                {
                    observer.OnCompleted();
                }
                else
                {
                    observer.OnNext(state.list.GetAt(tracker.CurrentIndex));
                    tracker.SetOriginalIndexAndResetCurrent(tracker.CurrentIndex + 1);
                    continueExecution((state.list, tracker));
                }
            }
            catch (Exception e)
            {
                observer.OnError(e);
            }
        }

        protected override void LongExecution((IBaseListCollection<T> list, ListCollectionIndexOffsetTracker<T> tracker) state, IObserver<T> observer, ICancelable cancelable)
        {
            var tracker = state.tracker;
            try
            {
                while (!cancelable.IsDisposed)
                {
                    if (tracker.CurrentIndex >= state.list.Count)
                    {
                        observer.OnCompleted();
                        break;
                    }
                    else
                    {
                        observer.OnNext(state.list.GetAt(tracker.CurrentIndex));
                        tracker.SetOriginalIndexAndResetCurrent(tracker.CurrentIndex + 1);
                    }
                }
            }
            catch (Exception e)
            {
                observer.OnError(e);
            }
        }
    }

    #endregion

    #region InfiniteLiveListObservable

    internal class InfiniteLiveListObservable<T> : ScheduledObservable<T, (IBaseListCollection<T> list, ListCollectionIndexOffsetTracker<T> tracker, TimeSpan timeout)>
    {
        private const int STARTING_INDEX = 0;
        private static readonly TimeSpan DEFAULT_EX_TIMEOUT = TimeSpan.FromMilliseconds(100);

        private static ListCollectionIndexOffsetTracker<T> CreateTracker(IBaseListCollection<T> list, int startingIndex)
        {
            var tracker = new ListCollectionIndexOffsetTracker<T>()
            {
                OriginalIndex = startingIndex
            };
            list.CollectionChangedEvent += tracker.HandleEvent;
            list.CollectionChangedEvent += (_, e) =>
            {
                if (e != null)
                {
                    lock (tracker)
                    {
                        Monitor.Pulse(tracker);
                    }
                }
            };
            return tracker;
        }

        public InfiniteLiveListObservable(IBaseListCollection<T> list, IScheduler scheduler, int startingIndex = STARTING_INDEX) : this(list, scheduler, DEFAULT_EX_TIMEOUT, startingIndex)
        {
        }

        public InfiniteLiveListObservable(IBaseListCollection<T> list, IScheduler scheduler, TimeSpan executionTimeout, int startingIndex = STARTING_INDEX) : 
            base(ObservableType.LiveUpdating, scheduler, () => (list, CreateTracker(list, startingIndex), executionTimeout))
        {
            SupportsLongRunning = true;
        }

        protected override void IndividualExecution((IBaseListCollection<T> list, ListCollectionIndexOffsetTracker<T> tracker, TimeSpan timeout) state, IObserver<T> observer, 
            Action<(IBaseListCollection<T> list, ListCollectionIndexOffsetTracker<T> tracker, TimeSpan timeout)> continueExecution)
        {
            var tracker = state.tracker;
            try
            {
                if (tracker.CurrentIndex >= state.list.Count)
                {
                    lock (tracker)
                    {
                        // We do a timeout here because we have no knowedlge of the associated disposable. It's been abstracted away.
                        // So we'd rather do a light busy loop then some crazy logic to get the external disposable
                        Monitor.Wait(tracker, state.timeout);
                    }
                }
                else
                {
                    observer.OnNext(state.list.GetAt(tracker.CurrentIndex));
                    tracker.SetOriginalIndexAndResetCurrent(tracker.CurrentIndex + 1);
                }
                continueExecution((state.list, tracker, state.timeout));
            }
            catch (Exception e)
            {
                observer.OnError(e);
            }
        }

        protected override void LongExecution((IBaseListCollection<T> list, ListCollectionIndexOffsetTracker<T> tracker, TimeSpan timeout) state, IObserver<T> observer, ICancelable cancelable)
        {
            var tracker = state.tracker;
            var pollCancelable = !(cancelable is CancellationDisposable);
            try
            {
                // If possible, we'd rather go to sleep the thread completely so long as we can notify it that the thread was canceled
                if (cancelable is CancellationDisposable cancellationDisposable && !cancellationDisposable.IsDisposed)
                {
                    cancellationDisposable.Token.Register(cancellationState =>
                    {
                        lock (cancellationState)
                        {
                            Monitor.Pulse(tracker);
                        }
                    }, tracker);
                }

                while (!cancelable.IsDisposed)
                {
                    if (tracker.CurrentIndex < state.list.Count)
                    {
                        observer.OnNext(state.list.GetAt(tracker.CurrentIndex));
                        tracker.SetOriginalIndexAndResetCurrent(tracker.CurrentIndex + 1);
                    }
                    else
                    {
                        lock (tracker)
                        {
                            // If we can notify that the thread was canceled, then we don't need to poll. If not, we need to poll the cancelable if it's been disposed.
                            if (pollCancelable)
                            {
                                Monitor.Wait(tracker, state.timeout);
                            }
                            else
                            {
                                Monitor.Wait(tracker);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                observer.OnError(e);
            }
        }
    }

    #endregion
}
