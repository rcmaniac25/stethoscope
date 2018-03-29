namespace LogTracker.Common
{
    public interface ILogParser
    {
        void Parse(string logFile);

        void SetRegistry(ILogRegistry registry);
        void SetConfig(LogConfig config);
    }
}
