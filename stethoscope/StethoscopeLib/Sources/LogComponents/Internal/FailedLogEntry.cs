using Stethoscope.Common;

using System;
using System.Collections.Generic;
using System.Text;

namespace Stethoscope.Log.Internal
{
    /// <summary>
    /// Failed log entry stored in a log registry. Failed logs are logs that were not able to be parsed. This log entry represents the parts of the log that were parsed.
    /// </summary>
    public class FailedLogEntry : IInternalLogEntry, IEquatable<FailedLogEntry>
    {
        [Flags]
        private enum StatusFlags : byte
        {
            Unknown = 0,

            TimestampSet = 1 << 0,
            HasBeenNotified = 1 << 1
        }

        private Dictionary<LogAttribute, object> attributes = new Dictionary<LogAttribute, object>();
        private StatusFlags flags;

        /// <summary>
        /// Timestamp of the log entry.
        /// </summary>
        public DateTime Timestamp => (DateTime)attributes[LogAttribute.Timestamp];
        /// <summary>
        /// The specific log message.
        /// </summary>
        public string Message => (string)attributes[LogAttribute.Message];
        /// <summary>
        /// If this log entry is a valid log entry. Valid=was able to be parsed completely. Always returns <c>false</c>.
        /// </summary>
        public bool IsValid => false;
        /// <summary>
        /// If the timestamp of the log entry changed.
        /// </summary>
        public bool HasTimestampChanged
        {
            get
            {
                return (flags & StatusFlags.TimestampSet) == StatusFlags.TimestampSet;
            }
            private set
            {
                if (value)
                {
                    flags |= StatusFlags.TimestampSet;
                }
                else
                {
                    flags &= ~StatusFlags.TimestampSet;
                }
            }
        }
        /// <summary>
        /// If the log registry was notified for this log. Can alternativly be seen as "is log done being processed"
        /// </summary>
        public bool IsRegistryNotified
        {
            get
            {
                return (flags & StatusFlags.HasBeenNotified) == StatusFlags.HasBeenNotified;
            }
            private set
            {
                if (value)
                {
                    flags |= StatusFlags.HasBeenNotified;
                }
                else
                {
                    flags &= ~StatusFlags.HasBeenNotified;
                }
            }
        }
        /// <summary>
        /// The unique ID of the log entry.
        /// </summary>
        public Guid ID { get; } = Guid.NewGuid();
        /// <summary>
        /// If the log entry doesn't have any attributes.
        /// </summary>
        public bool IsEmpty => attributes.Count == 0;

        /// <summary>
        /// Resets <see cref="HasTimestampChanged"/> to <c>false</c>.
        /// </summary>
        public void ResetTimestampChanged()
        {
            HasTimestampChanged = false;
        }

        /// <summary>
        /// Specify that the log registry was notified for this log..
        /// </summary>
        /// <seealso cref="IsRegistryNotified"/>
        public void LogRegistryNotified()
        {
            IsRegistryNotified = true;
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
        public void AddAttribute(LogAttribute attribute, object value)
        {
            attributes.Add(attribute, value);
            if (attribute == LogAttribute.Timestamp)
            {
                HasTimestampChanged = true;
            }
        }

        /// <summary>
        /// If an object is equal to this log entry.
        /// </summary>
        /// <param name="obj">The object to compare to this log entry.</param>
        /// <returns><c>true</c> if the object and this log entry are equal, <c>false</c> otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is FailedLogEntry failedEntry)
            {
                return Equals(failedEntry);
            }
            return false;
        }

        private bool AttributeEquals(FailedLogEntry other)
        {
            // Increasingly complex tests

            if (attributes.Count != other.attributes.Count)
            {
                return false;
            }

            foreach (var key in attributes.Keys)
            {
                if (!other.attributes.ContainsKey(key))
                {
                    return false;
                }
            }

            foreach (var kv in attributes)
            {
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
        public bool Equals(FailedLogEntry other)
        {
            if (other != null)
            {
                return other.ID == ID || AttributeEquals(other);
            }
            return false;
        }

        /// <summary>
        /// Get the hash code of this log entry.
        /// </summary>
        /// <returns>The hash code of this log entry.</returns>
        public override int GetHashCode() => 91450537 ^ attributes.GetHashCode();

        /// <summary>
        /// Get a string representation of this log entry.
        /// </summary>
        /// <returns>A string representation of this log entry.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("Failed: ");
            var offset = 0;

            if (attributes.ContainsKey(LogAttribute.Timestamp))
            {
                if (attributes.ContainsKey(LogAttribute.Message))
                {
                    sb.AppendFormat("{0} : {1}", attributes[LogAttribute.Timestamp], attributes[LogAttribute.Message]);
                    offset = 2;
                }
                else
                {
                    sb.Append(attributes[LogAttribute.Timestamp]);
                    offset = 1;
                }
            }
            else if (attributes.ContainsKey(LogAttribute.Message))
            {
                sb.Append(attributes[LogAttribute.Message]);
                offset = 1;
            }
            
            if (offset != 0)
            {
                sb.Append("; ");
            }
            if (attributes.Count > offset)
            {
                sb.AppendFormat("attributes={0}", attributes.Count - offset);
            }
            return sb.ToString();
        }

        //XXX does this need tests? it's mostly just copy-paste code from LogEntry
    }
}
