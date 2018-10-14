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

        private readonly IRegistryStorage dataRegistryStorage;
        private readonly IBaseListCollection<ILogEntry> data;

        public ListStorageEvaluator(IRegistryStorage storage, IBaseListCollection<ILogEntry> data)
        {
            this.dataRegistryStorage = storage;
            this.data = data;
        }

        private IScheduler DataScheduler { get { return dataRegistryStorage.LogScheduler; } }

        public IObservable<T> Evaluate<T>(Expression expression, Type sourceType)
        {
            var schedulerToUse = DataScheduler ?? CurrentThreadScheduler.Instance;
            IObservable<ILogEntry> dataSourceObservable;
            
            var expressionToEvaluate = expression;

            // The expression must represent a query over the data source. 
            if (!IsQueryOverDataSource(expression))
            {
                dataSourceObservable = new LiveListObservable<ILogEntry>(LiveObservableType, data, schedulerToUse);
            }
            else
            {
                // We want to know if we can adjust the starting index of the data source
                var skipBuilder = new SkipCalculator();
                var skip = skipBuilder.CalculateSkip(expression);

                // Get an observable for the data
                if (skip.HasValue)
                {
                    dataSourceObservable = new LiveListObservable<ILogEntry>(LiveObservableType, data, schedulerToUse, skip.Value);

                    // Update the expression so it no longer includes the skips we previously counted
                    var modifier = new SkipTreeModifier();
                    expressionToEvaluate = modifier.Visit(expressionToEvaluate);
                }
                else
                {
                    dataSourceObservable = new LiveListObservable<ILogEntry>(LiveObservableType, data, schedulerToUse);
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
