using LogTracker.Common;
using LogTracker.Log.Internal;

using System;
using System.Collections.Generic;

namespace LogTracker.Log
{
    public class LogEntry : IMutableLogEntry
    {
        Lazy<DateTime> lazyTimestamp;
        Lazy<string> lazyMessage;

        public DateTime Timestamp
        {
            get
            {
                return lazyTimestamp.Value;
            }
        }

        public string Message
        {
            get
            {
                return lazyMessage.Value;
            }
        }

        private Dictionary<LogAttribute, object> attributes = new Dictionary<LogAttribute, object>();

        internal LogEntry(DateTime timestamp, string logMessage)
        {
            AddAttribute(LogAttribute.Timestamp, timestamp);
            AddAttribute(LogAttribute.Message, logMessage);

            lazyTimestamp = new Lazy<DateTime>(() => GetAttribute<DateTime>(LogAttribute.Timestamp), true);
            lazyMessage = new Lazy<string>(() => GetAttribute<string>(LogAttribute.Message), true);
        }

        public bool HasAttribute(LogAttribute attribute)
        {
            return attributes.ContainsKey(attribute);
        }

        public T GetAttribute<T>(LogAttribute attribute)
        {
            if (!attributes.ContainsKey(attribute))
            {
                throw new KeyNotFoundException();
            }

            return (T)attributes[attribute];
        }

        public void AddAttribute(LogAttribute attribute, object value)
        {
            attributes.Add(attribute, value);
        }
    }
}
