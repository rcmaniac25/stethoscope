using Stethoscope.Collections;
using Stethoscope.Common;
using Stethoscope.Reactive;
using Stethoscope.Reactive.Linq;
using Stethoscope.Reactive.Linq.Internal;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;

namespace Stethoscope.Log.Internal.Storage.Linq
{
    internal class ListStorageEvaluator : IObservableEvaluator
    {
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
            // The expression must represent a query over the data source. 
            if (!IsQueryOverDataSource(expression))
            {
                throw new InvalidOperationException("No query over the data source was specified.");
            }

            // We want to know if we can adjust the starting index of the data source
            var skipBuilder = new SkipCalculator();
            var skip = skipBuilder.CalculateSkip(expression);
            
            // Get an observable for the data
            var schedulerToUse = DataScheduler ?? CurrentThreadScheduler.Instance;
            IObservable<ILogEntry> dataSourceObservable;
            if (skip.HasValue)
            {
                dataSourceObservable = new LiveListObservable<ILogEntry>(ObservableType.LiveUpdating, data, schedulerToUse, skip.Value);
            }
            else
            {
                dataSourceObservable = new LiveListObservable<ILogEntry>(ObservableType.LiveUpdating, data, schedulerToUse);
            }

            // Update the expression so it no longer includes the skips we previously counted
            var expressionToEvaluate = expression;
            if (skip.HasValue)
            {
                throw new NotImplementedException();
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
