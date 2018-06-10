﻿using System;
using System.Reactive.Linq;

using Stethoscope.Common;

namespace Stethoscope.Log.Internal
{
    public interface IRegistryStorage
    {
        IQbservable<ILogEntry> Entries { get; }
        int Count { get; }
        LogAttribute SortAttribute { get; }

        Guid AddLogSorted(ILogEntry entry);
        void Clear();

        //XXX: close/shutdown?
    }
}
