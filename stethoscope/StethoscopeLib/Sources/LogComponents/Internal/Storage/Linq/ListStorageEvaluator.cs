using Metrics;

using Stethoscope.Collections;
using Stethoscope.Common;
using Stethoscope.Reactive;
using Stethoscope.Reactive.Linq;
using Stethoscope.Reactive.Linq.Internal;

using System;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Stethoscope.Log.Internal.Storage.Linq
{
    internal class ListStorageEvaluator : IObservableEvaluator
    {
        private const ObservableType LiveObservableType = ObservableType.LiveUpdating;

        private static readonly Counter expressionMethodCounter = Metric.Context("ListStorageEvaluator").Counter("Expression Methods", Unit.Commands, tags: "storage, expression, evaluator, list, method");

        private readonly IRegistryStorage dataRegistryStorage;
        private readonly IBaseListCollection<ILogEntry> data;
        private readonly ExpressionMethodVisitor<int> metricCollector = new ExpressionMethodVisitor<int>(); // Can't be static in-case async operation is used. Hopefully evaluate is never invoked async.

        public ListStorageEvaluator(IRegistryStorage storage, IBaseListCollection<ILogEntry> data)
        {
            this.dataRegistryStorage = storage;
            this.data = data;

            this.metricCollector.MethodVisitHandler = (mexp, d, unused, visit) =>
            {
                var method = mexp.Method;
                if (method.IsGenericMethod)
                {
                    method = method.GetGenericMethodDefinition();
                }
                expressionMethodCounter.Increment(method.ToString());
                visit(mexp.Arguments[0]);
                return mexp;
            };
        }

        private IScheduler DataScheduler { get { return dataRegistryStorage.LogScheduler; } }

        public IObservable<T> Evaluate<T>(Expression expression, Type sourceType)
        {
            var schedulerToUse = DataScheduler ?? CurrentThreadScheduler.Instance;
            IObservable<ILogEntry> dataSourceObservable = new LiveListObservable<ILogEntry>(LiveObservableType, data, schedulerToUse);

            // Gather metrics on the expression being processed.
            metricCollector.Visit(expression, 0);

            var expressionToEvaluate = expression;

            // The expression must represent a query over the data source.
            if (IsQueryOverDataSource(expression))
            {
                // We want to know if we can adjust the starting index of the data source
                var skipProcessor = new SkipProcessor();
                var (updatedExpression, skipCount) = skipProcessor.Process(expression);
                expressionToEvaluate = updatedExpression;

                // Get an observable for the data
                if (skipCount.HasValue)
                {
                    dataSourceObservable = new LiveListObservable<ILogEntry>(LiveObservableType, data, schedulerToUse, skipCount.Value);
                }
            }

            // Replace the data source instance with the observable
            var qDataSource = dataSourceObservable.AsQbservable();
            var sourceReplacer = new DataSourceReplacer(qDataSource, sourceType);
            var finalExpression = sourceReplacer.Visit(expressionToEvaluate);

            // Evaluate or build to get our observable
            return qDataSource.Provider.CreateQuery<T>(finalExpression);
        }
        
        private static bool IsQueryOverDataSource(Expression expression)
        {
            // From https://msdn.microsoft.com/en-us/library/bb546158.aspx

            // If expression represents an unqueried IQbservable data source instance, 
            // expression is of type ConstantExpression, not MethodCallExpression. 
            return expression is MethodCallExpression;
        }

        private class DataSourceReplacer : ExpressionVisitor
        {
            private readonly IQbservable<ILogEntry> dataSource;
            private readonly Type typeToReplace;

            internal DataSourceReplacer(IQbservable<ILogEntry> qbservable, Type typeReplace)
            {
                dataSource = qbservable;
                typeToReplace = typeReplace;
            }

            protected override Expression VisitConstant(ConstantExpression c)
            {
                if (c.Type == typeToReplace)
                {
                    return Expression.Constant(dataSource);
                }
                return c;
            }
        }
    }
}
