namespace LogTracker.Common
{
    public interface ILogEntry
    {
        bool HasAttribute(LogAttribute attribute);
        T GetAttribute<T>(LogAttribute attribute);
    }
}
