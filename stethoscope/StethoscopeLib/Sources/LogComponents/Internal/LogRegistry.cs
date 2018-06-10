using Stethoscope.Common;
using Stethoscope.Log.Internal;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Stethoscope.Log.Internal
{
    public class LogRegistry : ILogRegistry
    {
#if false

        private class IlogEntryComparer : IComparer<ILogEntry>
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

        //TODO: replace logs with an observable
        private List<ILogEntry> logs = new List<ILogEntry>();
        private readonly IlogEntryComparer logEntryComparer = new IlogEntryComparer();

        public int LogCount
        {
            get
            {
                return logs.Count;
            }
        }

        private void AddLogSorted(ILogEntry entry)
        {
            lock (logs)
            {
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
            }
        }

        private void AddLogUnsorted(ILogEntry entry)
        {
            lock (logs)
            {
                logs.Add(entry);
            }
        }

        private void SortUnsortedLog(FailedLogEntry failedLog)
        {
            lock (logs)
            {
                //XXX depending on the size of the log list, this could be a really long search. Perhaps have a function that marks the start of some parsing, or a general checkpoint, since all failed parses will be added to the end of the list. This way we can offset the search.
                var index = logs.FindIndex(en => en is IInternalLogEntry internalEntry && internalEntry.ID == failedLog.ID);
                if (index < 0)
                {
                    throw new ArgumentException("Failed log does not exist in this registry", "entry");
                }
                logs.RemoveAt(index);
            }
            if (!failedLog.IsEmpty)
            {
                AddLogSorted(failedLog);
            }
        }

        public void Clear()
        {
            lock (logs)
            {
                logs.Clear();
            }
        }

        public static IObservable<IGroupedObservable<object, ILogEntry>> GetLogBy(LogAttribute attribute, IObservable<ILogEntry> entries)
        {
            if (attribute == LogAttribute.Timestamp)
            {
                return null;
            }
            return entries.Where(log =>
            {
                if (log.HasAttribute(attribute))
                {
                    if (!log.IsValid && attribute == LogAttribute.Message && !(log.GetAttribute<object>(attribute) is string))
                    {
                        // Special case to ensure that message and timestamp (which doesn't work here anyway) match the expected types
                        return false;
                    }
                    return true;
                }
                return false;
            }).GroupBy(log => log.GetAttribute<object>(attribute));
        }

        public IObservable<IGroupedObservable<object, ILogEntry>> GetBy(LogAttribute attribute) => GetLogBy(attribute, logs.ToObservable());

        public IObservable<ILogEntry> GetByTimetstamp()
        {
            return logs.ToObservable().Where(log =>
            {
                // Special case to ensure that message (which doesn't apply here anyway) and timestamp match the expected types
                // For failed logs, timestamps that haven't been sorted are ignored.
                var isInvalid = !log.IsValid &&
                    ((log is IInternalLogEntry internalLog && internalLog.HasTimestampChanged) ||
                    !log.HasAttribute(LogAttribute.Timestamp) || !(log.GetAttribute<object>(LogAttribute.Timestamp) is DateTime));

                return !isInvalid;
            });
        }
#endif

        private readonly IRegistryStorage storage;

        public LogRegistry(IRegistryStorage storage)
        {
            if (storage == null)
            {
                throw new ArgumentNullException("storage");
            }
            if (storage.SortAttribute != LogAttribute.Timestamp)
            {
                throw new ArgumentException("Storage must sort logs by Timestamp", "storage");
            }
            this.storage = storage;
        }

        public int LogCount => storage.Count;

        private void AddLogSorted(ILogEntry entry)
        {
            storage.AddLogSorted(entry);
        }

        private void AddLogUnsorted(ILogEntry entry)
        {
            //TODO
        }

        public ILogEntry AddLog(string timestamp, string message)
        {
            if (timestamp == null)
            {
                throw new ArgumentNullException("timestamp");
            }
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            
            if (!DateTime.TryParse(timestamp, out DateTime time))
            {
                throw new ArgumentException("Could not parse timestamp", "timestamp");
            }
            var entry = new LogEntry(time, message);
            AddLogSorted(entry);
            return entry;
        }

        public ILogEntry AddFailedLog()
        {
            var entry = new FailedLogEntry();
            AddLogUnsorted(entry);
            return entry;
        }

        private void SortUnsortedLog(FailedLogEntry failedLog)
        {
            //TODO
        }

        public void NotifyFailedLogParsed(ILogEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }
            if (!(entry is FailedLogEntry))
            {
                throw new ArgumentException("Entry is not a failed log", "entry");
            }

            var failedLog = (FailedLogEntry)entry;
            if (failedLog.HasTimestampChanged || failedLog.IsEmpty)
            {
                SortUnsortedLog(failedLog);
            }
            failedLog.ResetTimestampChanged();
        }

        public bool AddValueToLog(ILogEntry entry, LogAttribute attribute, object value)
        {
            if (value == null)
            {
                return false;
            }
            if (entry is IInternalLogEntry internalEntry && !entry.HasAttribute(attribute))
            {
                internalEntry.AddAttribute(attribute, value);
                return true;
            }
            return false;
        }

        public void Clear() => storage.Clear();
        
        //XXX still needed?
        public static IObservable<IGroupedObservable<object, ILogEntry>> GetLogBy(LogAttribute attribute, IObservable<ILogEntry> entries)
        {
            if (attribute == LogAttribute.Timestamp)
            {
                return null;
            }
            return entries.Where(log =>
            {
                if (log.HasAttribute(attribute))
                {
                    if (!log.IsValid && attribute == LogAttribute.Message && !(log.GetAttribute<object>(attribute) is string))
                    {
                        // Special case to ensure that message and timestamp (which doesn't work here anyway) match the expected types
                        return false;
                    }
                    return true;
                }
                return false;
            }).GroupBy(log => log.GetAttribute<object>(attribute));
        }

        //XXX still needed?
        public IObservable<IGroupedObservable<object, ILogEntry>> GetBy(LogAttribute attribute)
        {
            //TODO
            return null;
        }

        //XXX still needed?
        public IObservable<ILogEntry> GetByTimetstamp()
        {
            //TODO
            return null;
        }

        /* TODO: LINQ support
         * 
         * from log in registry
         * where log.Message.Contains("base64")
         * select new {log.Timestamp, log.ThreadID, log.Message};
         */

        //TODO: special get functions - get by function, get by thread ID, get by <key>, etc.?
    }
}
