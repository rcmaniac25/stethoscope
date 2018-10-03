using Stethoscope.Collections;
using Stethoscope.Common;
using Stethoscope.Log.Internal;
using Stethoscope.Reactive.Linq;

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
            throw new NotImplementedException();
        }
    }
}
