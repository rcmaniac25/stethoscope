using System;
using System.Collections.Generic;
using System.Text;
using System.Reactive;
using System.Reactive.Linq;
using System.Linq;
using System.Linq.Expressions;

namespace Stethoscope.Reactive.Linq
{
    // #1

    internal class BaseQbservable<T> : IQbservable<T>
    {
        public BaseQbservable()
        {
            Provider = new BaseQbservableProvider();
            Expression = Expression.Constant(this);
        }

        public BaseQbservable(BaseQbservableProvider provider, Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            if (!typeof(IQbservable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException(nameof(expression));
            }

            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression;
        }

        public Expression Expression { get; private set; }

        public IQbservableProvider Provider { get; private set; }

        public Type ElementType => typeof(T);

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return (((BaseQbservableProvider)Provider).Execute<IObservable<T>>(Expression)).Subscribe(observer);
        }
    }

    // #2

    class BaseQbservableProvider : IQbservableProvider
    {
        public IQbservable<T> CreateQuery<T>(Expression expression)
        {
            return new BaseQbservable<T>(this, expression);
        }

        public T Execute<T>(Expression expression)
        {
            var isObservable = (typeof(T).Name == "IObservable`1");

            return (T)BaseQbservableContext.Execute(expression, isObservable);
        }
    }

    // #3

    class BaseQbservableContext
    {
        internal static object Execute(Expression expression, bool isObservable)
        {
            if (!IsQueryOverDataSource(expression))
            {
                throw new InvalidProgramException("No query over the data source was specified.");
            }

            //TODO

            throw new NotImplementedException();
        }

        private static bool IsQueryOverDataSource(Expression expression)
        {
            // If expression represents an unqueried IQbservable data source instance, 
            // expression is of type ConstantExpression, not MethodCallExpression. 
            return (expression is MethodCallExpression);
        }
    }
}
