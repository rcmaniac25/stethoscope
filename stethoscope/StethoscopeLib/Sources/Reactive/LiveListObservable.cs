using Stethoscope.Collections;

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;

namespace Stethoscope.Reactive
{
    struct LiveListState<T>
    {
        public IBaseListCollection<T> List;
        public ListCollectionIndexOffsetTracker<T> Tracker;
        public TimeSpan Timeout;
        public ObservableType ExecutionType;
        
        public static LiveListState<T> CreateState(ObservableType executionType, IBaseListCollection<T> list, int startingIndex, TimeSpan timeout)
        {
            var tracker = new ListCollectionIndexOffsetTracker<T>()
            {
                OriginalIndex = startingIndex
            };
            //XXX This screams "race conditon"... (doesn't actually work... will need to change)
            var addTrackerLate = list.Count == 0;
            list.CollectionChangedEvent += (_, e) =>
            {
                if (e != null)
                {
                    lock (tracker)
                    {
                        Monitor.Pulse(tracker);
                        if (addTrackerLate)
                        {
                            addTrackerLate = false;
                            list.CollectionChangedEvent += tracker.HandleEvent;
                        }
                    }
                }
            };
            if (!addTrackerLate)
            {
                list.CollectionChangedEvent += tracker.HandleEvent;
            }
            return new LiveListState<T>()
            {
                List = list,
                Tracker = tracker,
                Timeout = timeout,
                ExecutionType = executionType
            };
        }
    }

    internal class LiveListObservable<T> : ScheduledObservable<T, LiveListState<T>>
    {
        private const int STARTING_INDEX = 0;
        private static readonly TimeSpan DEFAULT_EX_TIMEOUT = TimeSpan.FromMilliseconds(100);
        
        public LiveListObservable(ObservableType type, IBaseListCollection<T> list, IScheduler scheduler, int startingIndex = STARTING_INDEX) : this(type, list, scheduler, DEFAULT_EX_TIMEOUT, startingIndex)
        {
        }

        public LiveListObservable(ObservableType type, IBaseListCollection<T> list, IScheduler scheduler, TimeSpan executionTimeout, int startingIndex = STARTING_INDEX) :
            base(type, scheduler, () => LiveListState<T>.CreateState(type, list, startingIndex, executionTimeout))
        {
            SupportsLongRunning = true;
        }

        protected override void IndividualExecution(LiveListState<T> state, IObserver<T> observer,
            Action<LiveListState<T>> continueExecution)
        {
            try
            {
                if (state.Tracker.CurrentIndex >= state.List.Count)
                {
                    if (state.ExecutionType == ObservableType.InfiniteLiveUpdating)
                    {
                        lock (state.Tracker)
                        {
                            // We do a timeout here because we have no knowedlge of the associated disposable. It's been abstracted away.
                            // So we'd rather do a light busy loop then some crazy logic to get the external disposable
                            Monitor.Wait(state.Tracker, state.Timeout);
                        }
                    }
                    else
                    {
                        observer.OnCompleted();
                        return;
                    }
                }
                else
                {
                    observer.OnNext(state.List.GetAt(state.Tracker.CurrentIndex));
                    state.Tracker.SetOriginalIndexAndResetCurrent(state.Tracker.CurrentIndex + 1);
                }
                continueExecution(state);
            }
            catch (Exception e)
            {
                observer.OnError(e);
            }
        }

        protected override void LongExecution(LiveListState<T> state, IObserver<T> observer, ICancelable cancelable)
        {
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
                            Monitor.Pulse(cancellationState);
                        }
                    }, state.Tracker);
                }

                while (!cancelable.IsDisposed)
                {
                    if (state.Tracker.CurrentIndex < state.List.Count)
                    {
                        observer.OnNext(state.List.GetAt(state.Tracker.CurrentIndex));
                        state.Tracker.SetOriginalIndexAndResetCurrent(state.Tracker.CurrentIndex + 1);
                    }
                    else if (state.ExecutionType == ObservableType.InfiniteLiveUpdating)
                    {
                        lock (state.Tracker)
                        {
                            // If we can notify that the thread was canceled, then we don't need to poll. If not, we need to poll the cancelable if it's been disposed.
                            if (pollCancelable)
                            {
                                Monitor.Wait(state.Tracker, state.Timeout);
                            }
                            else
                            {
                                Monitor.Wait(state.Tracker);
                            }
                        }
                    }
                    else
                    {
                        observer.OnCompleted();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                observer.OnError(e);
            }
        }
    }
}
