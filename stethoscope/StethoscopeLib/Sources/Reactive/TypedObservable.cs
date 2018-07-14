using System.Reactive;
using System.Reactive.Concurrency;

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
}
