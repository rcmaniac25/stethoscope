using Metrics;

using Stethoscope.Common;
using Stethoscope.Reactive;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Stethoscope.Log.Internal
{
    /// <summary>
    /// Standard log registry for storing all log entries.
    /// </summary>
    public class LogRegistry : ILogRegistry
    {
        private static readonly Counter dateTimeParseFailureCounter;
        private static readonly Meter addLogMeter;
        private static readonly Meter addFailedLogMeter;
        private static readonly Meter addCloneLogMeter;
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
            addCloneLogMeter = logRegistryContext.Meter("CloneLog", Unit.Calls, tags: "log, registry, add, log, clone");
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

        /// <summary>
        /// Create a LogRegistry.
        /// </summary>
        /// <param name="storage">The actual storage object that will hold all log entries.</param>
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
            this.LogScheduler = TaskPoolScheduler.Default;
        }

        /// <summary>
        /// Scheduler for retrieving logs from the registry.
        /// </summary>
        public IScheduler LogScheduler
        {
            get
            {
                return storage.LogScheduler;
            }
            set
            {
                storage.LogScheduler = value;
            }
        }

        /// <summary>
        /// Number of logs, both stored and in processing, contained by this registry.
        /// </summary>
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

        /// <summary>
        /// Add a new log to the registry.
        /// </summary>
        /// <param name="timestamp">String representation of a timestamp.</param>
        /// <param name="message">The message that was logged for this entry.</param>
        /// <returns>The created log entry.</returns>
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

        /// <summary>
        /// Add a "failed" log to the registry. This is often a log entry that could not be parsed fully by the log parser.
        /// </summary>
        /// <returns>A failed log entry. <see cref="ILogEntry.IsValid"/> will return <c>false</c> for these entries. Once a log is done being parsed, <see cref="NotifyFailedLogParsed(ILogEntry)"/> should be called.</returns>
        /// <remarks><see cref="LogRegistryExtensions.GetBy(ILogRegistry, LogAttribute)"/> might not return these entries. <see cref="LogRegistryExtensions.GetByTimetstamp"/> will only return these entries if the timestamp exists.</remarks>
        /// <seealso cref="LogParserFailureHandling"/>
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

        /// <summary>
        /// Clone a log entry into this log registry.
        /// </summary>
        /// <param name="entry">The log entry to clone.</param>
        /// <returns>The cloned log entry.</returns>
        public ILogEntry CloneLog(ILogEntry entry)
        {
            if (entry == null)
            {
                return null;
            }
            
            if (ContainsLog(entry))
            {
                return entry;
            }

            var addToRegistry = false;
            var addToProcessing = false;
            IInternalLogEntry cloneEntry;
            if (entry is LogEntry || (entry.IsValid && !(entry is FailedLogEntry)))
            {
                cloneEntry = new LogEntry(entry.Timestamp, entry.Message);
                addToRegistry = true;
            }
            else
            {
                cloneEntry = new FailedLogEntry();
                if (entry.HasAttribute(LogAttribute.Timestamp))
                {
                    cloneEntry.AddAttribute(LogAttribute.Timestamp, entry.GetAttribute<object>(LogAttribute.Timestamp));

                    if (entry is IInternalLogEntry entryInternal)
                    {
                        if (entryInternal.HasTimestampChanged)
                        {
                            // there's a timestamp, but it's still marked as changed. We get an exception if add gets invoked again, so it has only been added once and hasn't been notifed, which means it's still in processing
                            addToProcessing = true;
                        }
                        else
                        {
                            // there's a timestamp, but it hasn't been marked as changed. It means the timestamp change has been reset which only happens in notify
                            cloneEntry.ResetTimestampChanged();
                            addToRegistry = true;
                        }
                    }
                }
                if (entry.HasAttribute(LogAttribute.Message))
                {
                    cloneEntry.AddAttribute(LogAttribute.Message, entry.GetAttribute<object>(LogAttribute.Message));
                }

                // Edge cases
                // - no timestamp, not empty : it's not empty, but it lacks a timestamp. It may or may not have been processed
                // - no timestamp, empty : empty doesn't mean it hasn't been processed (which means it's not in the process list) but also doesn't mean it has been processed (which means it can be discarded)
            }

            foreach (var attribute in Enum.GetValues(typeof(LogAttribute)).Cast<LogAttribute>())
            {
                if (attribute == LogAttribute.Timestamp || attribute == LogAttribute.Message)
                {
                    continue;
                }
                if (entry.HasAttribute(attribute))
                {
                    cloneEntry.AddAttribute(attribute, entry.GetAttribute<object>(attribute));
                }
            }

            if (addToRegistry)
            {
                storage.AddLogSorted(cloneEntry);
            }
            else if (addToProcessing)
            {
                lock (logsBeingProcessed)
                {
                    logsBeingProcessed.Add(cloneEntry);
                    logsBeingProcessedCounter.Increment();
                }
            }
            return cloneEntry;
        }

        // Get if the log entry is from this registry
        private bool ContainsLog(ILogEntry entry)
        {
            if (entry is IInternalLogEntry internalLog)
            {
                if (!internalLog.IsValid)
                {
                    lock (logsBeingProcessed)
                    {
                        // Shouldn't be many failed logs
                        if (logsBeingProcessed.Count(log => log.ID == internalLog.ID) >= 1)
                        {
                            return true;
                        }
                    }
                }
                return storage.Entries.TakeUntil(log => ((IInternalLogEntry)log).ID != internalLog.ID).Count(log => ((IInternalLogEntry)log).ID == internalLog.ID).Wait() >= 1; //XXX should we do "wait" or can we tie it into some other aspect of the program? We should at least not wait indefinitely
            }
            // LogEntry and FailedLogEntry are both IInternalLogEntry, so if it isn't one of those, then it's a 3rd party ILogEntry and wasn't created by this registry
            return false; //XXX Should we check for timestamp or something so we don't clone a 3rd party log entry into storage that has the same timestamp?
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

        /// <summary>
        /// Notify the registry that the failed log has finished getting parsed. This does not indicate it has been successfully parsed, just that it is done parsing.
        /// </summary>
        /// <param name="entry">The failed log entry. Any other log entry will throw an exception.</param>
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
            if (failedLog.HasTimestampChanged || failedLog.IsEmpty) //As we sort by timestamp, we either want a timestamp to be set or for it to be empty (so it can be ignored and removed from the processing list)
            {
                notifyParsedProcessingCounter.Increment();
                ProcessingComplete(failedLog);
            }
            failedLog.ResetTimestampChanged();
        }

        /// <summary>
        /// Add an attribute value to the log entry.
        /// </summary>
        /// <param name="entry">The entry to add an attribute to.</param>
        /// <param name="attribute">The attribute to add to.</param>
        /// <param name="value">The attribute value to add.</param>
        /// <returns><c>true</c> if the entry was added. <c>false</c> if otherwise.</returns>
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

        /// <summary>
        /// Remove all logs from this registry.
        /// </summary>
        public void Clear()
        {
            clearCounter.Increment();
            lock (logsBeingProcessed)
            {
                storage.Clear();
                logsBeingProcessed.Clear();
            }
        }

        /// <summary>
        /// Queriable log entries.
        /// </summary>
        public IQbservable<ILogEntry> Logs
        {
            get
            {
                logObservableRequestedCounter.Increment();
                lock (logsBeingProcessed)
                {
                    IObservable<ILogEntry> logsBeingProcessObservable;
                    if (LogScheduler != null)
                    {
                        logsBeingProcessObservable = logsBeingProcessed.ToObservable(ObservableType.LiveUpdating, LogScheduler);
                    }
                    else
                    {
                        logsBeingProcessObservable = logsBeingProcessed.ToObservable(ObservableType.LiveUpdating);
                    }

                    return storage.Entries.Concat(logsBeingProcessObservable);
                }
            }
        }
    }
}
