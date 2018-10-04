using Stethoscope.Collections;
using Stethoscope.Common;
using Stethoscope.Log.Internal;
using Stethoscope.Reactive;
using Stethoscope.Reactive.Linq;
using Stethoscope.Reactive.Linq.Internal;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Text;

namespace Stethoscope.LogComponents.Internal.Storage.Linq
{
    internal class ListStorageEvaluator : IObservableEvaluator
    {
        private readonly IRegistryStorage dataRegistryStorage;
        private readonly IBaseReadWriteListCollection<ILogEntry> data;

        public ListStorageEvaluator(IRegistryStorage storage, IBaseReadWriteListCollection<ILogEntry> data)
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
            
#if false //XXX (see log)
            // We only deal with ILogEntry, but we don't want to hardcode evaluators (it produces annoying compile errors)
            if (typeof(T) != typeof(ILogEntry))
            {
                throw new InvalidOperationException("Evaluation only supported for ILogEntry types");
            }
            var dataSource = UnsafeUnbox<T, ILogEntry>(data);
#endif

            // We want to know if we can adjust the starting index of the data source
            var skipBuilder = new SkipCalculator();
            var skip = skipBuilder.CalculateSkip(expression);

#if false
            // Get an observable for the data
            var schedulerToUse = DataScheduler ?? CurrentThreadScheduler.Instance;
            IObservable<T> dataSourceObservable;
            if (skip.HasValue)
            {
                dataSourceObservable = new LiveListObservable<T>(ObservableType.LiveUpdating, dataSource, schedulerToUse, skip.Value);
            }
            else
            {
                dataSourceObservable = new LiveListObservable<T>(ObservableType.LiveUpdating, dataSource, schedulerToUse);
            }
#endif

            // Update the expression so it no longer includes the skips we previously counted
            throw new NotImplementedException();

            // Replace the data source instance with the observable
            //TODO

            // Evaluate or build to get our observable
            //TODO
        }

        private static IBaseReadWriteListCollection<T> UnsafeUnbox<T, S>(IBaseReadWriteListCollection<S> data)
        {
            object obj = data;
            return (IBaseReadWriteListCollection<T>)obj;
        }

        private static bool IsQueryOverDataSource(Expression expression)
        {
            // From https://msdn.microsoft.com/en-us/library/bb546158.aspx

            // If expression represents an unqueried IQbservable data source instance, 
            // expression is of type ConstantExpression, not MethodCallExpression. 
            return expression is MethodCallExpression;
        }
    }
}
