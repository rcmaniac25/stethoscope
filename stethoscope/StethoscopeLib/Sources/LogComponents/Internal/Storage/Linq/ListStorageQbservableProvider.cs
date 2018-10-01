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
    internal class ListStorageQbservableProvider : IQbservableProvider
    {
        public IQbservable<TResult> CreateQuery<TResult>(Expression expression)
        {
            throw new NotImplementedException();
        }
    }
}
