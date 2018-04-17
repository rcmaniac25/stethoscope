using System.Collections.Generic;

namespace LogTracker.Common
{
    public interface ILogRegistry
    {
        ILogEntry AddLog(string timestamp, string message);
        bool AddValueToLog(ILogEntry entry, LogAttribute attribute, object value);

        IDictionary<object, IEnumerable<ILogEntry>> GetBy(LogAttribute attribute);
        IEnumerable<ILogEntry> GetByTimetstamp();
    }
}
