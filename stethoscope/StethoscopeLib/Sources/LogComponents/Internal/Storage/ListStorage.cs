using Metrics;

using Stethoscope.Common;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Stethoscope.Log.Internal.Storage
{
    /// <summary>
    /// Registry storage that internally stores data in a <see cref="System.Collections.Generic.List{T}"/>.
    /// </summary>
    public class ListStorage : IRegistryStorage
    {
        #region LogEntryComparer

        private class LogEntryComparer : IComparer<ILogEntry>
        {
            private DateTime? GetTimestamp(ILogEntry entry)
            {
                if (entry.IsValid)
                {
                    return entry.GetAttribute<DateTime>(LogAttribute.Timestamp);
                }

                if (entry.HasAttribute(LogAttribute.Timestamp))
                {
                    var timestamp = entry.GetAttribute<object>(LogAttribute.Timestamp);
                    if (timestamp is DateTime ts)
                    {
                        return ts;
                    }
                }

                return null;
            }

            public int Compare(ILogEntry x, ILogEntry y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }
                else if (x == null)
                {
                    return -1;
                }
                else if (y == null)
                {
                    return 1;
                }

                var xt = GetTimestamp(x);
                var yt = GetTimestamp(y);

                if (xt.HasValue && yt.HasValue)
                {
                    return xt.Value.CompareTo(yt.Value);
                }
                else if (xt.HasValue)
                {
                    return 1;
                }
                else if (yt.HasValue)
                {
                    return -1;
                }
                return 0;
            }
        }

        #endregion

        private static readonly Counter unsupportedLogEntryCounter;
        private static readonly Counter logCounter;
        private static readonly Timer addLogWithLockTimer;
        private static readonly Timer addLogWithoutLockTimer;
        private static readonly Counter clearCounter;

        static ListStorage()
        {
            var listStorageContext = Metric.Context("ListStorage");
            unsupportedLogEntryCounter = listStorageContext.Counter("Unsupported Log Entry", Unit.Errors, "registry, storage, error, log, entry, unsupported");
            logCounter = listStorageContext.Counter("Log Count", Unit.Items, "registry, storage, log, entry");
            addLogWithLockTimer = listStorageContext.Timer("AddLogSorted with Lock", Unit.Calls, tags: "registry, storage, log, entry, lock, add");
            addLogWithoutLockTimer = listStorageContext.Timer("AddLogSorted", Unit.Calls, tags: "registry, storage, log, entry, add");
            clearCounter = listStorageContext.Counter("Clear", Unit.Calls, "registry, storage");
        }

        private List<ILogEntry> logs = new List<ILogEntry>();
        private readonly LogEntryComparer logEntryComparer = new LogEntryComparer();

        /// <summary>
        /// Access the log entries stored in this.
        /// </summary>
        public IQbservable<ILogEntry> Entries => logs.ToObservable().AsQbservable(); //XXX This won't work with "Insert" (or "Add" for that instance). This is basically saying "foreach(var log in logs) { OnNext(log); }" and enumerations don't like iteration + changes

        /// <summary>
        /// The number of logs stored.
        /// </summary>
        public int Count
        {
            get
            {
                lock (logs)
                {
                    return logs.Count;
                }
            }
        }

        /// <summary>
        /// Logs are sorted by timestamp right now.
        /// </summary>
        //XXX this should probably be set-able, but not for now
        public LogAttribute SortAttribute => LogAttribute.Timestamp;

        /// <summary>
        /// Add a log to the storage.
        /// </summary>
        /// <param name="entry">The log to store.</param>
        /// <returns>The GUID that internally references the log entry.</returns>
        public Guid AddLogSorted(ILogEntry entry)
        {
            if (!(entry is IInternalLogEntry))
            {
                unsupportedLogEntryCounter.Increment();

                //XXX
                throw new ArgumentException("Log type is not supported (right now)", nameof(entry));
            }
            using (addLogWithLockTimer.NewContext())
            {
                lock (logs)
                {
                    using (addLogWithoutLockTimer.NewContext())
                    {
                        //XXX need to make sure this will continue to work

                        // From https://stackoverflow.com/a/22801345/492347
                        if (logs.Count == 0 || logEntryComparer.Compare(logs[logs.Count - 1], entry) <= 0)
                        {
                            logs.Add(entry);
                        }
                        else if (logEntryComparer.Compare(logs[0], entry) >= 0)
                        {
                            logs.Insert(0, entry);
                        }
                        else
                        {
                            var index = logs.BinarySearch(entry, logEntryComparer);
                            if (index < 0)
                            {
                                index = ~index;
                            }
                            logs.Insert(index, entry);
                        }
                        logCounter.Increment();
                    }
                }
            }
            return ((IInternalLogEntry)entry).ID;
        }

        /// <summary>
        /// Clear all logs from this storage.
        /// </summary>
        public void Clear()
        {
            clearCounter.Increment();

            lock (logs)
            {
                //XXX
                logs.Clear();
            }
        }
    }
}
