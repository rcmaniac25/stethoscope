using System;
using System.Collections.Generic;
using System.Text;

using Stethoscope.Common;

namespace Stethoscope.Log.Internal
{
    public class FailedLogEntry : IInternalLogEntry, IEquatable<FailedLogEntry>
    {
        private Dictionary<LogAttribute, object> attributes = new Dictionary<LogAttribute, object>();

        public DateTime Timestamp => (DateTime)attributes[LogAttribute.Timestamp];
        public string Message => (string)attributes[LogAttribute.Message];
        public bool IsValid => false;
        public bool HasTimestampChanged { get; private set; }
        public Guid ID { get; } = Guid.NewGuid();
        public bool IsEmpty => attributes.Count == 0;

        public void ResetTimestampChanged()
        {
            HasTimestampChanged = false;
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

        public void AddAttribute(LogAttribute attribute, object value)
        {
            attributes.Add(attribute, value);
            if (attribute == LogAttribute.Timestamp)
            {
                HasTimestampChanged = true;
            }
        }
        
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

        public bool Equals(FailedLogEntry other)
        {
            if (other != null)
            {
                return other.ID == ID || AttributeEquals(other);
            }
            return false;
        }

        public override int GetHashCode() => 91450537 ^ attributes.GetHashCode();

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
