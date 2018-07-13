using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Stethoscope.Reactive
{
    internal class TypedObservable<T> : ObservableBase<T>
    {
        protected TypedObservable(ObservableType type)
        {
            Type = type;
        }

        public ObservableType Type { get; private set; }

        protected override IDisposable SubscribeCore(IObserver<T> observer)
        {
            throw new NotImplementedException();
        }
    }
}
