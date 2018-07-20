using Stethoscope.Collections;

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

namespace Stethoscope.Reactive
{
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
}
