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
                throw new ArgumentNullException(nameof(storage));
            }
            if (storage.SortAttribute != LogAttribute.Timestamp)
            {
                throw new ArgumentException("Storage must sort logs by Timestamp", nameof(storage));
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
                throw new ArgumentNullException(nameof(timestamp));
            }
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            
            if (!DateTime.TryParse(timestamp, out DateTime time))
            {
                //TODO: record stat about failure
                throw new ArgumentException("Could not parse timestamp", nameof(timestamp));
            }
            //TODO: record stat about function used
            var entry = new LogEntry(time, message);
            storage.AddLogSorted(entry);
            return entry;
        }

        public ILogEntry AddFailedLog()
        {
            //TODO: record stat about function used
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
                    throw new ArgumentException("Failed log does not exist in this registry", "entry"); // Argument name comes from NotifyFailedLogParsed
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
                throw new ArgumentNullException(nameof(entry));
            }
            if (!(entry is FailedLogEntry))
            {
                //TODO: record stat about failure
                throw new ArgumentException("Entry is not a failed log", nameof(entry));
            }

            //TODO: record stat about function used
            var failedLog = (FailedLogEntry)entry;
            if (failedLog.HasTimestampChanged || failedLog.IsEmpty)
            {
                //TODO: record stat about timestamp/empty
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
            //TODO: record stat about function and attribute used
            if (entry is IInternalLogEntry internalEntry && !entry.HasAttribute(attribute))
            {
                //TODO: record stat about adding to entry
                internalEntry.AddAttribute(attribute, value);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            //TODO: record stat about function used
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
                //TODO: record stat about function used and how many "being processed" logs exist
                lock (logsBeingProcessed)
                {
                    return storage.Entries.Concat(logsBeingProcessed.ToObservable());
                }
            }
        }
    }
}
