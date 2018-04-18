using LogTracker.Common;
using LogTracker.Log.Internal;

using System;
using System.Collections.Generic;

namespace LogTracker.Log
{
    public class LogEntry : IMutableLogEntry, IComparable<LogEntry>, IEquatable<LogEntry>
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
            if (logMessage == null)
            {
                throw new ArgumentNullException("logMessage");
            }

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

        public int CompareTo(LogEntry other)
        {
            if (other == null)
            {
                return 1;
            }
            return Timestamp.CompareTo(other.Timestamp);
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is LogEntry)
            {
                return Equals((LogEntry)obj);
            }
            return false;
        }

        public bool Equals(LogEntry other)
        {
            if (other != null)
            {
                return other.Timestamp == Timestamp && other.Message == Message;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Timestamp.GetHashCode() * 31 + Message.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Timestamp} : {Message}";
        }
    }
}
