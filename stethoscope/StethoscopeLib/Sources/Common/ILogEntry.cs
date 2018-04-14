using System;

namespace LogTracker.Common
{
    public interface ILogEntry
    {
        DateTime Timestamp { get; }
        string Message { get; }

        bool HasAttribute(LogAttribute attribute);
        T GetAttribute<T>(LogAttribute attribute);
    }
}
