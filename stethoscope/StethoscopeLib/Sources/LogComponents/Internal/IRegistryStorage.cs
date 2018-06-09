using System;
using System.Reactive.Linq;

using Stethoscope.Common;

namespace Stethoscope.Log.Internal
{
    public interface IRegistryStorage
    {
        IQbservable<ILogEntry> Entries { get; }

        int Count { get; }

        Guid AddLog(ILogEntry entry);
        //XXX: update?
        void Clear();

        //XXX: close/shutdown?
    }
}
