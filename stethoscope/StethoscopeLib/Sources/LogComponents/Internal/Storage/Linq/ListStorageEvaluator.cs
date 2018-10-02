using Stethoscope.Collections;
using Stethoscope.Common;
using Stethoscope.Log.Internal;
using Stethoscope.Reactive.Linq;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Stethoscope.LogComponents.Internal.Storage.Linq
{
    internal class ListStorageEvaluator : IObservableEvaluator
    {
        public ListStorageEvaluator(IRegistryStorage storage, IBaseReadWriteListCollection<ILogEntry> data)
        {
            //TODO
        }

        public IObservable<T> Evaluate<T>(Expression expression)
        {
            throw new NotImplementedException();
        }
    }
}
