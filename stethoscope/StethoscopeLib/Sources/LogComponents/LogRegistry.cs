﻿using LogTracker.Common;
using LogTracker.Log.Internal;

using System;
using System.Collections.Generic;

namespace LogTracker.Log
{
    public class LogRegistry : ILogRegistry
    {
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
                    if (timestamp is DateTime)
                    {
                        return (DateTime)timestamp;
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

        private List<ILogEntry> logs = new List<ILogEntry>();
        private readonly IlogEntryComparer logEntryComparer = new IlogEntryComparer();

        private void AddLogSorted(ILogEntry entry)
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
            //AddLogSorted(entry);
            //TODO: implement, but also need a way to verify that empty logs are removed (maybe with a "notify done parsing" call). Needs to also take all logs that have since gotten there timestamp to be sorted properly
            return entry;
        }

        public bool AddValueToLog(ILogEntry entry, LogAttribute attribute, object value)
        {
            if (!(entry is IMutableLogEntry))
            {
                return false;
            }
            if (entry.HasAttribute(attribute) || entry == null || value == null)
            {
                return false;
            }
            (entry as IMutableLogEntry).AddAttribute(attribute, value);
            return true;
        }

        public void Clear() => logs.Clear();

        public static IDictionary<object, IEnumerable<ILogEntry>> GetLogBy(LogAttribute attribute, IEnumerable<ILogEntry> entries)
        {
            //XXX Initial implementation... should stream instead of building a dictionary (like when doing a groupBy)
            if (attribute == LogAttribute.Timestamp)
            {
                return null;
            }
            var tmpResult = new Dictionary<object, List<ILogEntry>>();
            foreach (var log in entries)
            {
                if (log.HasAttribute(attribute))
                {
                    var key = log.GetAttribute<object>(attribute);
                    if (!log.IsValid && attribute == LogAttribute.Message && !(key is string))
                    {
                        // Special case to ensure that message and timestamp (which doesn't work here anyway) match the expected types
                        continue;
                    }
                    if (!tmpResult.ContainsKey(key))
                    {
                        tmpResult.Add(key, new List<ILogEntry>());
                    }
                    tmpResult[key].Add(log);
                }
            }

            var result = new Dictionary<object, IEnumerable<ILogEntry>>();
            foreach (var kv in tmpResult)
            {
                result.Add(kv.Key, kv.Value.AsReadOnly());
            }
            return result;
        }

        public IDictionary<object, IEnumerable<ILogEntry>> GetBy(LogAttribute attribute) => GetLogBy(attribute, logs);

        public IEnumerable<ILogEntry> GetByTimetstamp()
        {
            foreach (var log in logs)
            {
                if (!log.IsValid && 
                    (!log.HasAttribute(LogAttribute.Timestamp) || !(log.GetAttribute<object>(LogAttribute.Timestamp) is DateTime)))
                {
                    // Special case to ensure that message (which doesn't apply here anyway) and timestamp match the expected types
                    continue;
                }
                yield return log;
            }
        }

        //TODO: special get functions - get by function, get by thread ID, get by <key>, etc.
        /* TODO: LINQ support?
         * 
         * from log in registry
         * where log.Message.Contains("base64")
         * select new {log.Timestamp, log.ThreadID, log.Message};
         */
    }
}
