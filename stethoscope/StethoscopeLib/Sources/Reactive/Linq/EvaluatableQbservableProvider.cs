using System;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace Stethoscope.Reactive.Linq
{
    internal class EvaluatableQbservableProvider : IQbservableProvider
    {
        public IObservableEvaluator Evaluator { get; private set; }
        public Type SourceType { get; private set; }

        public EvaluatableQbservableProvider(IObservableEvaluator evaluator, Type sourceType)
        {
            Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            SourceType = sourceType;
        }

        public IQbservable<TResult> CreateQuery<TResult>(Expression expression)
        {
            return new EvaluatableQbservable<TResult>(this, expression);
        }
    }
}
