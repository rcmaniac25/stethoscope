using LogTracker.Common;

using System;

namespace LogTracker.Log.Internal
{
    public interface IMutableLogEntry : ILogEntry
    {
        Guid ID { get; }
        bool HasTimestampChanged { get; }

        void AddAttribute(LogAttribute attribute, object value);

        void ResetTimestampChanged();
    }
}
