using System;
using System.Collections.Generic;
using System.Text;

using LogTracker.Common;

namespace LogTracker.Log.Internal
{
    public class FailedLogEntry : IMutableLogEntry, IComparable<FailedLogEntry>, IEquatable<FailedLogEntry>
    {
        public DateTime Timestamp => (DateTime)attributes[LogAttribute.Timestamp];

        public string Message => (string)attributes[LogAttribute.Message];

        public bool IsValid => false;

        private Dictionary<LogAttribute, object> attributes = new Dictionary<LogAttribute, object>();

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

        public int CompareTo(ILogEntry other)
        {
            if (other is FailedLogEntry)
            {
                return CompareTo((FailedLogEntry)other);
            }
            //TODO: what is proper for IComparable? Does A.CompareTo(B) = 0 => A.Equals(B) = true?
            throw new NotImplementedException();
        }

        public int CompareTo(FailedLogEntry other)
        {
            if (other == null)
            {
                return 1;
            }
            return Timestamp.CompareTo(other.Timestamp); //TODO: need proper comparision
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is FailedLogEntry)
            {
                return Equals((FailedLogEntry)obj);
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
                return AttributeEquals(other);
            }
            return false;
        }

        public override int GetHashCode() => 91450537 ^ attributes.GetHashCode();

        public override string ToString()
        {
            //TODO: rewrite
            var sb = new StringBuilder();
            sb.AppendFormat("{0} : {1}", Timestamp, Message);
            if (attributes.Count > 2)
            {
                sb.AppendFormat("; attributes={0}", attributes.Count - 2);
            }
            return sb.ToString();
        }

        //XXX does this need tests? it's mostly just copy-paste code from LogEntry
    }
}
