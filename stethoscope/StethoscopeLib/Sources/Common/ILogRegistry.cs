using System.Collections.Generic;

namespace LogTracker.Common
{
    public interface ILogRegistry
    {
        ILogEntry AddLog(string timestamp, string message);
        bool AddValueToLog(ILogEntry entry, LogAttribute attribute, object value);

        IDictionary<object, ILogEntry[]> GetBy(LogAttribute attribute);
    }
}
