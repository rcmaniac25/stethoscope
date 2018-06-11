using Stethoscope.Common;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Stethoscope.Log.Internal
{
    public class LogRegistry : ILogRegistry
    {
        private readonly IRegistryStorage storage;
        private readonly List<IInternalLogEntry> logsBeingProcessed = new List<IInternalLogEntry>();

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

        public int LogCount
        {
            get
            {
                lock (logsBeingProcessed)
                {
                    return storage.Count + logsBeingProcessed.Count;
                }
            }
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
            storage.AddLogSorted(entry);
            return entry;
        }

        public ILogEntry AddFailedLog()
        {
            var entry = new FailedLogEntry();
            lock (logsBeingProcessed)
            {
                logsBeingProcessed.Add(entry);
            }
            return entry;
        }

        private void ProcessingComplete(FailedLogEntry failedLog)
        {
            lock (logsBeingProcessed)
            {
                var index = logsBeingProcessed.FindIndex(en => en.ID == failedLog.ID);
                if (index < 0)
                {
                    throw new ArgumentException("Failed log does not exist in this registry", "entry");
                }
                logsBeingProcessed.RemoveAt(index);
                if (!failedLog.IsEmpty)
                {
                    storage.AddLogSorted(failedLog);
                }
            }
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
                ProcessingComplete(failedLog);
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

        public void Clear()
        {
            lock (logsBeingProcessed)
            {
                storage.Clear();
                logsBeingProcessed.Clear();
            }
        }

        public IQbservable<ILogEntry> Logs
        {
            get
            {
                lock (logsBeingProcessed)
                {
                    return storage.Entries.Concat(logsBeingProcessed.ToObservable());
                }
            }
        }
    }
}
