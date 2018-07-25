using Stethoscope.Collections;

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

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

    internal class InfiniteLiveListObservable<T> : ScheduledObservable<T, (IBaseListCollection<T> list, ListCollectionIndexOffsetTracker<T> tracker)>
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

        public InfiniteLiveListObservable(IBaseListCollection<T> list, IScheduler scheduler, int startingIndex = STARTING_INDEX) : base(ObservableType.LiveUpdating, scheduler, () => (list, CreateTracker(list, startingIndex)))
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
                    //TODO: sleep a short bit, or until notification
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
                    if (tracker.CurrentIndex < state.list.Count)
                    {
                        observer.OnNext(state.list.GetAt(tracker.CurrentIndex));
                        tracker.SetOriginalIndexAndResetCurrent(tracker.CurrentIndex + 1);
                    }
                    else
                    {
                        //TODO: sleep until notification
                    }
                }
            }
            catch (Exception e)
            {
                observer.OnError(e);
            }
        }

        private void HandleListEvent(object sender, ListCollectionEventArgs<T> e)
        {
            if (sender != null && e != null)
            {
                //TODO: find the matching list and notify it (better to make it a state-object so it doesn't rely on some global-state which will get screwed up when multiple subscriptions to the same list are made)
            }
        }
    }

    #endregion
}
