using Metrics;

using Stethoscope.Common;

using System;
using System.Reactive.Linq;

namespace Stethoscope.Log.Internal.Storage
{
    public class NullStorage : IRegistryStorage
    {
        private static readonly Counter unsupportedLogEntryCounter;
        private static readonly Counter logCounter;
        private static readonly Counter clearCounter;

        static NullStorage()
        {
            var listStorageContext = Metric.Context("NullStorage");
            unsupportedLogEntryCounter = listStorageContext.Counter("Unsupported Log Entry", Unit.Errors, "registry, storage, error, log, entry, unsupported");
            logCounter = listStorageContext.Counter("Log Count", Unit.Items, "registry, storage, log, entry");
            clearCounter = listStorageContext.Counter("Clear", Unit.Calls, "registry, storage");
        }

        public NullStorage(LogAttribute sortReturn)
        {
            SortAttribute = sortReturn;
        }

        public IQbservable<ILogEntry> Entries => Observable.Empty<ILogEntry>().AsQbservable();

        public int Count { get; private set; }

        public LogAttribute SortAttribute { get; set; }

        public Guid AddLogSorted(ILogEntry entry)
        {
            if (entry is IInternalLogEntry internalLog)
            {
                Count++;
                logCounter.Increment();

                return internalLog.ID;
            }
            else
            {
                unsupportedLogEntryCounter.Increment();
            }
            return Guid.NewGuid();
        }

        public void Clear()
        {
            Count = 0;

            clearCounter.Increment();
        }
    }
}
