using Metrics;

using Stethoscope.Common;

using System;
using System.Reactive.Linq;

namespace Stethoscope.Log.Internal.Storage
{
    /// <summary>
    /// A storage container that doesn't store logs. It simply counts valid logs.
    /// </summary>
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

        /// <summary>
        /// Create a new NullStorage.
        /// </summary>
        /// <param name="sortReturn">The attribute to return in <see cref="SortAttribute"/></param>
        public NullStorage(LogAttribute sortReturn)
        {
            SortAttribute = sortReturn;
        }

        /// <summary>
        /// An empty <see cref="IQbservable{T}"/>
        /// </summary>
        public IQbservable<ILogEntry> Entries => Observable.Empty<ILogEntry>().AsQbservable();

        /// <summary>
        /// The number of logs added.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// An unused attribute.
        /// </summary>
        public LogAttribute SortAttribute { get; set; }

        /// <summary>
        /// Count supported logs.
        /// </summary>
        /// <param name="entry">The log to "add"</param>
        /// <returns>Valid GUID if counted, or an empty GUID if not.</returns>
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
            return Guid.Empty;
        }

        /// <summary>
        /// Reset the log counter to 0.
        /// </summary>
        public void Clear()
        {
            Count = 0;

            clearCounter.Increment();
        }
    }
}
