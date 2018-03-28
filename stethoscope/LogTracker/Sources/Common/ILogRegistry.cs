using LogTracker.Log; //XXX temp

namespace LogTracker.Common
{
    public interface ILogRegistry
    {
        ILogEntry AddLog(string timestamp, string message);
        bool AddValueToLog(ILogEntry entry, LogAttribute attribute, object value);
    }
}
