using System;
using System.Collections.Generic;

namespace LogTracker
{
    public enum LogAttribute
    {
        Timestamp,
        Message
    }

    public class LogEntry
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
            if (attributes.ContainsKey(attribute))
            {
                var att = attributes[attribute];
                if (att is T)
                {
                    return (T)att;
                }
            }
            return default(T);
        }

        internal void AddAttribute(LogAttribute attribute, object value)
        {
            attributes.Add(attribute, value);
        }
    }
}
