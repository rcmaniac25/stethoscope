namespace LogTracker
{
    public enum LogParserErrors
    {
        OK,
        
        ConfigNotInitialized,

        RegistryNotSet, // Optional, since the log parser could simply be doing a pass to try and parse data

        MissingTimestamp,
        MissingMessage
    }

    public interface ILogParser<T>
    {
        LogParserErrors ProcessLog(T rawLogElement);

        void SetRegistry(LogRegistry registry);
        void SetConfig(LogConfig config);
    }
}
