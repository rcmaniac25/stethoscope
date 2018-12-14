using Stethoscope.Common;

using System;
using System.Collections.Generic;
using System.Text;

namespace Stethoscope.Log.Internal
{
    /// <summary>
    /// Standard log entry stored in a log registry.
    /// </summary>
    public class LogEntry : IInternalLogEntry, IEquatable<LogEntry>
    {
        Lazy<DateTime> lazyTimestamp;
        Lazy<string> lazyMessage;
        private Dictionary<LogAttribute, object> attributes = new Dictionary<LogAttribute, object>();

        /// <summary>
        /// Timestamp of the log entry.
        /// </summary>
        public DateTime Timestamp => lazyTimestamp.Value;
        /// <summary>
        /// The specific log message.
        /// </summary>
        public string Message => lazyMessage.Value;
        /// <summary>
        /// If this log entry is a valid log entry. Valid=was able to be parsed completely. Always returns <c>true</c>.
        /// </summary>
        public bool IsValid => true;
        /// <summary>
        /// If the timestamp of the log entry changed. Always returns <c>false</c>.
        /// </summary>
        public bool HasTimestampChanged => false;
        /// <summary>
        /// The unique ID of the log entry.
        /// </summary>
        public Guid ID { get; } = Guid.NewGuid();
        /// <summary>
        /// If the log entry doesn't have any attributes. Always returns <c>false</c>.
        /// </summary>
        public bool IsEmpty => false;
        /// <summary>
        /// Get or set the registry that owns the log entry.
        /// </summary>
        public ILogRegistry Owner { get; set; }

        internal LogEntry(DateTime timestamp, string logMessage)
        {
            if (logMessage == null)
            {
                throw new ArgumentNullException(nameof(logMessage));
            }

            AddAttribute(LogAttribute.Timestamp, timestamp);
            AddAttribute(LogAttribute.Message, logMessage);

            lazyTimestamp = new Lazy<DateTime>(() => GetAttribute<DateTime>(LogAttribute.Timestamp), true);
            lazyMessage = new Lazy<string>(() => GetAttribute<string>(LogAttribute.Message), true);
        }

        /// <summary>
        /// No-op. Resets <see cref="HasTimestampChanged"/> to <c>false</c>.
        /// </summary>
        public void ResetTimestampChanged()
        {
        }

        /// <summary>
        /// Get if a specific log attribute exists.
        /// </summary>
        /// <param name="attribute">The attribute to get if it exists.</param>
        /// <returns><c>true</c> if the attribute exists. <c>false</c> if otherwise.</returns>
        public bool HasAttribute(LogAttribute attribute) => attributes.ContainsKey(attribute);

        /// <summary>
        /// Get the specific log attribute.
        /// </summary>
        /// <typeparam name="T">The type of the log entry. Exception thrown if type mismatch.</typeparam>
        /// <param name="attribute">The attribute to get. Exception thrown if it doesn't exist.</param>
        /// <returns>The attribute value.</returns>
        /// <seealso cref="HasAttribute(LogAttribute)"/>
        public T GetAttribute<T>(LogAttribute attribute)
        {
            if (!attributes.ContainsKey(attribute))
            {
                throw new KeyNotFoundException();
            }

            return (T)attributes[attribute];
        }

        /// <summary>
        /// Add an attribute to the log entry. Shouldn't be used directly. Use <see cref="ILogRegistry.AddValueToLog(ILogEntry, LogAttribute, object)"/> instead.
        /// </summary>
        /// <param name="attribute">The attribute to add.</param>
        /// <param name="value">The value of the attribute.</param>
        public void AddAttribute(LogAttribute attribute, object value) => attributes.Add(attribute, value);

        /// <summary>
        /// If an object is equal to this log entry.
        /// </summary>
        /// <param name="obj">The object to compare to this log entry.</param>
        /// <returns><c>true</c> if the object and this log entry are equal, <c>false</c> otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is LogEntry entry)
            {
                return Equals(entry);
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

        /// <summary>
        /// If a log entry is equal to this log entry.
        /// </summary>
        /// <param name="other">The log entry to compare to this log entry.</param>
        /// <returns><c>true</c> if the log entry and this log entry are equal, <c>false</c> otherwise.</returns>
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

        /// <summary>
        /// Get the hash code of this log entry.
        /// </summary>
        /// <returns>The hash code of this log entry.</returns>
        public override int GetHashCode() => 1852909 ^ attributes.GetHashCode();

        /// <summary>
        /// Get a string representation of this log entry.
        /// </summary>
        /// <returns>A string representation of this log entry.</returns>
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
