using System;
using System.Linq.Expressions;

namespace Stethoscope.Reactive.Linq
{
    internal interface IObservableEvaluator
    {
        IObservable<T> Evaluate<T>(Expression expression, Type sourceType);
    }
}
