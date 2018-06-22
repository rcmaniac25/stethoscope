using Metrics;

using Stethoscope.Common;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Stethoscope.Log.Internal
{
    public class LogRegistry : ILogRegistry
    {
        private static readonly Counter dateTimeParseFailureCounter;
        private static readonly Meter addLogMeter;
        private static readonly Meter addFailedLogMeter;
        private static readonly Counter logsBeingProcessedCounter;
        private static readonly Counter notifyParsedInvalidLogTypeCounter;
        private static readonly Counter notifyParsedCounter;
        private static readonly Counter notifyParsedProcessingCounter;
        private static readonly Counter addValueToLogCounter;
        private static readonly Counter addValueToLogAddingCounter;
        private static readonly Counter clearCounter;
        private static readonly Counter logObservableRequestedCounter;

        static LogRegistry()
        {
            var logRegistryContext = Metric.Context("LogRegistry");
            dateTimeParseFailureCounter = logRegistryContext.Counter("AddLog DateTime Parse Failures", Unit.Errors, "log, registry, add, parse, failure");
            addLogMeter = logRegistryContext.Meter("AddLog", Unit.Calls, tags: "log, registry, add, log");
            addFailedLogMeter = logRegistryContext.Meter("AddFailedLog", Unit.Calls, tags: "log, registry, add, log, failed");
            logsBeingProcessedCounter = logRegistryContext.Counter("Logs being Processed", Unit.Items, "log, registry, process, failed, items");
            notifyParsedInvalidLogTypeCounter = logRegistryContext.Counter("NotifyFailedLogParsed Invalid Log Type", Unit.Errors, "log, registry, notify, failed, invalid, type");
            notifyParsedCounter = logRegistryContext.Counter("NotifyFailedLogParsed", Unit.Calls, "log, registry, notify, failed");
            notifyParsedProcessingCounter = logRegistryContext.Counter("NotifyFailedLogParsed Processing", Unit.Events, "log, registry, notify, failed");
            addValueToLogCounter = logRegistryContext.Counter("AddValueToLog", Unit.Calls, "log, registry, add, value, log");
            addValueToLogAddingCounter = logRegistryContext.Counter("AddValueToLog (actually add to log)", Unit.Events, "log, registry, add, value, log");
            clearCounter = logRegistryContext.Counter("Clear", Unit.Calls, "log, registry, clear");
            logObservableRequestedCounter = logRegistryContext.Counter("Logs", Unit.Calls, "log, registry, logs");
        }

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

            addLogMeter.Mark();

            if (!DateTime.TryParse(timestamp, out DateTime time))
            {
                dateTimeParseFailureCounter.Increment();
                throw new ArgumentException("Could not parse timestamp", nameof(timestamp));
            }
            var entry = new LogEntry(time, message);
            storage.AddLogSorted(entry);
            return entry;
        }

        public ILogEntry AddFailedLog()
        {
            addFailedLogMeter.Mark();

            var entry = new FailedLogEntry();
            lock (logsBeingProcessed)
            {
                logsBeingProcessed.Add(entry);
                logsBeingProcessedCounter.Increment();
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
                logsBeingProcessedCounter.Decrement();
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
                notifyParsedInvalidLogTypeCounter.Increment();
                throw new ArgumentException("Entry is not a failed log", nameof(entry));
            }

            notifyParsedCounter.Increment();

            var failedLog = (FailedLogEntry)entry;
            if (failedLog.HasTimestampChanged || failedLog.IsEmpty)
            {
                notifyParsedProcessingCounter.Increment();
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
            addValueToLogCounter.Increment(attribute.ToString());

            if (entry is IInternalLogEntry internalEntry && !entry.HasAttribute(attribute))
            {
                addValueToLogAddingCounter.Increment();
                internalEntry.AddAttribute(attribute, value);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            clearCounter.Increment();
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
                logObservableRequestedCounter.Increment();
                lock (logsBeingProcessed)
                {
                    return storage.Entries.Concat(logsBeingProcessed.ToObservable());
                }
            }
        }
    }
}
