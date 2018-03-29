using LogTracker.Common;
using LogTracker.Log.Internal;

using System;
using System.Collections.Generic;

namespace LogTracker.Log
{
    public class LogRegistry : ILogRegistry
    {
        private List<LogEntry> logs = new List<LogEntry>();

        public ILogEntry AddLog(string timestamp, string message)
        {
            DateTime time;
            if (!DateTime.TryParse(timestamp, out time))
            {
                return null;
            }
            var entry = new LogEntry(time, message);
            logs.Add(entry);
            return entry;
        }

        public bool AddValueToLog(ILogEntry entry, LogAttribute attribute, object value)
        {
            if (attribute == LogAttribute.Message || 
                attribute == LogAttribute.Timestamp ||
                !(entry is IMutableLogEntry))
            {
                return false;
            }
            (entry as IMutableLogEntry).AddAttribute(attribute, value);
            return true;
        }

        public void Clear()
        {
            logs.Clear();
        }

        public static IDictionary<object, LogEntry[]> GetLogBy(LogAttribute attribute, IEnumerable<LogEntry> entries)
        {
            //XXX Initial implementation... should stream instead of building a dictionary (like when doing a groupBy)
            switch (attribute)
            {
                case LogAttribute.Message:
                case LogAttribute.Timestamp:
                    return null;
            }
            var tmpResult = new Dictionary<object, List<LogEntry>>();
            foreach (var log in entries)
            {
                if (log.HasAttribute(attribute))
                {
                    var key = log.GetAttribute<object>(attribute);
                    if (!tmpResult.ContainsKey(key))
                    {
                        tmpResult.Add(key, new List<LogEntry>());
                    }
                    tmpResult[key].Add(log);
                }
            }

            var result = new Dictionary<object, LogEntry[]>();
            foreach (var kv in tmpResult)
            {
                result.Add(kv.Key, kv.Value.ToArray());
            }
            return result;
        }

        public IDictionary<object, LogEntry[]> GetBy(LogAttribute attribute)
        {
            return GetLogBy(attribute, logs);
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
