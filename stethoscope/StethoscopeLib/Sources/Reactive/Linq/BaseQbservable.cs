using System;
using System.Collections.Generic;
using System.Text;
using System.Reactive;
using System.Reactive.Linq;
using System.Linq;
using System.Linq.Expressions;

namespace Stethoscope.Sources.Reactive.Linq
{
    internal class BaseQbservable<T> : IQbservable<T>
    {
        public Type ElementType => typeof(T);

        public Expression Expression => throw new NotImplementedException();

        public IQbservableProvider Provider => throw new NotImplementedException();

        public IDisposable Subscribe(IObserver<T> observer)
        {
            throw new NotImplementedException();
        }
    }

    internal class BaseQbservableProvider : IQbservableProvider
    {
        public IQbservable<TResult> CreateQuery<TResult>(Expression expression)
        {
            throw new NotImplementedException();
        }
    }
}
