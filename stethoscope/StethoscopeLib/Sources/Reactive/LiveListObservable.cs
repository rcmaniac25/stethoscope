using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

namespace Stethoscope.Reactive
{
    internal class LiveListObservable<T> : ScheduledObservable<T, (IList<T> list, int index)>
    {
        public LiveListObservable(IList<T> list, IScheduler scheduler) : base(ObservableType.LiveUpdating, scheduler, (list, 0))
        {
            SupportsLongRunning = true;
        }

        protected override void IndividualExecution((IList<T> list, int index) state, IObserver<T> observer, Action<(IList<T> list, int index)> continueExecution)
        {
            var currentIndex = state.index;
            try
            {
                if (currentIndex >= state.list.Count)
                {
                    observer.OnCompleted();
                }
                else
                {
                    observer.OnNext(state.list[currentIndex]);
                    continueExecution((state.list, currentIndex + 1));
                }
            }
            catch (Exception e)
            {
                observer.OnError(e);
            }
        }

        protected override void LongExecution((IList<T> list, int index) state, IObserver<T> observer, ICancelable cancelable)
        {
            var currentIndex = state.index;
            try
            {
                while (!cancelable.IsDisposed)
                {
                    if (currentIndex >= state.list.Count)
                    {
                        observer.OnCompleted();
                        break;
                    }
                    else
                    {
                        observer.OnNext(state.list[currentIndex++]);
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
