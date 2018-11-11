using System;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace Stethoscope.Reactive.Linq
{
    internal class EvaluatableQbservable<T> : IQbservable<T>
    {
        private IObservable<T> source;

        internal EvaluatableQbservable(IObservableEvaluator evaluator)
        {
            Provider = new EvaluatableQbservableProvider(evaluator, GetType());
            Expression = Expression.Constant(this);
        }

        internal EvaluatableQbservable(EvaluatableQbservableProvider provider, Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (!typeof(IQbservable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentException("Expression doesn't produce a type that is compatible with this qbservable's element type", nameof(expression));
            }

            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression;
        }

        public Type ElementType => typeof(T);
        public Expression Expression { get; private set; }
        public IQbservableProvider Provider { get; private set; }

        internal Expression EvaluatedExpression { get; private set; }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (source == null)
            {
                var evalProvider = (EvaluatableQbservableProvider)Provider;
                source = evalProvider.Evaluator.Evaluate<T>(Expression, evalProvider.SourceType);
                
#if DEBUG
                // For testing purposes
                if (source is IQbservable<T> qSource)
                {
                    EvaluatedExpression = qSource.Expression;
                }
#endif
            }
            return source.Subscribe(observer);
        }
    }
}
