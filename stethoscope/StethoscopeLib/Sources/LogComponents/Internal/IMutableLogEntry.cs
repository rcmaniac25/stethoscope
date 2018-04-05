using LogTracker.Common;

namespace LogTracker.Log.Internal
{
    public interface IMutableLogEntry : ILogEntry
    {
        void AddAttribute(LogAttribute attribute, object value);
    }
}
