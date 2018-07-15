using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

namespace Stethoscope.Reactive
{
    internal abstract class TypedObservable<T> : ObservableBase<T>
    {
        protected IScheduler scheduler;

        protected TypedObservable(ObservableType type, IScheduler scheduler)
        {
            this.Type = type;
            this.scheduler = scheduler;
        }

        public ObservableType Type { get; private set; }
    }

    internal abstract class ScheduledObservable<T, S> : TypedObservable<T>
    {
        private readonly S state;

        protected ScheduledObservable(ObservableType type, IScheduler scheduler, S state) : base(type, scheduler)
        {
            this.state = state;
        }

        protected bool SupportsLongRunning { get; set; }

        // Duplicate of System.Reactive.Linq.ObservableImpl.ToObservable's "sink" run function.
        protected override IDisposable SubscribeCore(IObserver<T> observer)
        {
            if (SupportsLongRunning)
            {
                var longRunning = scheduler.AsLongRunning();
                if (longRunning != null)
                {
                    return longRunning.ScheduleLongRunning((state, observer), SingleLongExecution);
                }
            }
            ICancelable disposable = new BooleanDisposable();
            scheduler.Schedule((state, observer, disposable), RecursiveExecution);
            return disposable;
        }

        private void SingleLongExecution((S innerState, IObserver<T> observable) state, ICancelable cancelable)
        {
            try
            {
                LongExecution(state.innerState, state.observable, cancelable);
            }
            catch (Exception e)
            {
                state.observable.OnError(e);
            }
        }

        // Based on the general flow of System.Reactive.Linq.ObservableImpl.ToObservable's "sink" LoopRec function.
        private void RecursiveExecution((S innerState, IObserver<T> observable, ICancelable cancelable) state, Action<(S innerState, IObserver<T> observable, ICancelable cancelable)> recurse)
        {
            if (state.cancelable.IsDisposed)
            {
                try
                {
                    IndividualExecutionCanceled(state.innerState);
                }
                catch
                {
                }
            }
            else
            {
                try
                {
                    IndividualExecution(state.innerState, state.observable, (newState) =>
                    {
                        recurse((newState, state.observable, state.cancelable));
                    });
                }
                catch (Exception e)
                {
                    state.observable.OnError(e);
                }
            }
        }

        protected virtual void LongExecution(S state, IObserver<T> observer, ICancelable cancelable)
        {
            throw new NotSupportedException("Developer didn't implement LongExecution function while setting SupportsLongRunning = true");
        }

        protected abstract void IndividualExecution(S state, IObserver<T> observer, Action<S> continueExecution);

        protected virtual void IndividualExecutionCanceled(S state)
        {
        }
    }
}
