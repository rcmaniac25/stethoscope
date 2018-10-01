using Stethoscope.Collections;
using Stethoscope.Common;
using Stethoscope.Log.Internal;

using System;
using System.Collections.Generic;
using System.Text;
using System.Reactive;
using System.Reactive.Linq;
using System.Linq;
using System.Linq.Expressions;

namespace Stethoscope.LogComponents.Internal.Storage.Linq
{
    internal class ListStorageQbservable : IQbservable<ILogEntry>
    {
        internal ListStorageQbservable(IRegistryStorage registryStorage, IBaseReadWriteListCollection<ILogEntry> internalStorage)
        {
            //TODO
            Provider = new ListStorageQbservableProvider();
            Expression = Expression.Constant(this);
        }

        public Type ElementType => typeof(ILogEntry);
        public Expression Expression { get; private set; }
        public IQbservableProvider Provider { get; private set; }

        public IDisposable Subscribe(IObserver<ILogEntry> observer)
        {
            return Provider.CreateQuery<ILogEntry>(Expression).Subscribe(observer);
        }
    }
}
