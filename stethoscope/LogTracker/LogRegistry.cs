﻿using System;
using System.Collections.Generic;

namespace LogTracker
{
    public class LogRegistry
    {
        private List<LogEntry> logs = new List<LogEntry>();

        public LogEntry AddLog(string timestamp, string message)
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

        public bool AddValueToLog(LogEntry entry, LogAttribute attribute, object value)
        {
            if (attribute == LogAttribute.Message || 
                attribute == LogAttribute.Timestamp)
            {
                return false;
            }
            entry.AddAttribute(attribute, value);
            return true;
        }

        public void Clear()
        {
            logs.Clear();
        }

        public static IDictionary<string, LogEntry[]> GetLogBy(LogAttribute attribute, IEnumerable<LogEntry> entries)
        {
            //XXX Initial implementation... should stream instead of building a dictionary (like when doing a groupBy)
            switch (attribute)
            {
                case LogAttribute.Message:
                case LogAttribute.Timestamp:
                    return null;
            }
            var tmpResult = new Dictionary<string, List<LogEntry>>();
            foreach (var log in entries)
            {
                if (log.HasAttribute(attribute))
                {
                    var key = log.GetAttribute<string>(attribute);
                    if (!tmpResult.ContainsKey(key))
                    {
                        tmpResult.Add(key, new List<LogEntry>());
                    }
                    tmpResult[key].Add(log);
                }
            }

            var result = new Dictionary<string, LogEntry[]>();
            foreach (var kv in tmpResult)
            {
                result.Add(kv.Key, kv.Value.ToArray());
            }
            return result;
        }

        public IDictionary<string, LogEntry[]> GetBy(LogAttribute attribute)
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
