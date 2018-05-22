using LogTracker.Common;
using LogTracker.Log.Internal;

using System;
using System.Collections.Generic;
using System.Text;

namespace LogTracker.Log
{
    public class LogEntry : IMutableLogEntry, IEquatable<LogEntry>
    {
        Lazy<DateTime> lazyTimestamp;
        Lazy<string> lazyMessage;
        private Dictionary<LogAttribute, object> attributes = new Dictionary<LogAttribute, object>();

        public DateTime Timestamp => lazyTimestamp.Value;
        public string Message => lazyMessage.Value;
        public bool IsValid => true;
        public bool HasTimestampChanged => false;
        public Guid ID { get; } = Guid.NewGuid();

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

        public void ResetTimestampChanged()
        {
        }

        public bool HasAttribute(LogAttribute attribute) => attributes.ContainsKey(attribute);

        public T GetAttribute<T>(LogAttribute attribute)
        {
            if (!attributes.ContainsKey(attribute))
            {
                throw new KeyNotFoundException();
            }

            return (T)attributes[attribute];
        }

        public void AddAttribute(LogAttribute attribute, object value) => attributes.Add(attribute, value);

        public override bool Equals(object obj)
        {
            if (obj != null && obj is LogEntry)
            {
                return Equals((LogEntry)obj);
            }
            return false;
        }

        private bool AttributeEquals(LogEntry other)
        {
            // Increasingly complex tests

            if (attributes.Count != other.attributes.Count)
            {
                return false;
            }
            if (attributes.Count == 2) // Already have (and tested) Timestamp and Message
            {
                // Early out
                return true;
            }

            foreach (var key in attributes.Keys)
            {
                // We test these already
                if (key == LogAttribute.Timestamp || key == LogAttribute.Message)
                {
                    continue;
                }
                if (!other.attributes.ContainsKey(key))
                {
                    return false;
                }
            }

            foreach (var kv in attributes)
            {
                // We test these already
                if (kv.Key == LogAttribute.Timestamp || kv.Key == LogAttribute.Message)
                {
                    continue;
                }
                var otherValue = other.attributes[kv.Key];
                if (otherValue == null && kv.Value == null)
                {
                    continue;
                }
                if (otherValue == null || !otherValue.Equals(kv.Value))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Equals(LogEntry other)
        {
            if (other != null)
            {
                return other.ID == ID ||
                    (other.attributes[LogAttribute.Timestamp].Equals(attributes[LogAttribute.Timestamp]) && // Internally, number comparsion: fast
                    other.attributes[LogAttribute.Message].Equals(attributes[LogAttribute.Message]) && // Internally, data comparison: O(n)
                    AttributeEquals(other)); // Slowest test (see above)
            }
            return false;
        }

        public override int GetHashCode() => 1852909 ^ attributes.GetHashCode();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} : {1}", Timestamp, Message);
            if (attributes.Count > 2)
            {
                sb.AppendFormat("; attributes={0}", attributes.Count - 2);
            }
            return sb.ToString();
        }
    }
}
