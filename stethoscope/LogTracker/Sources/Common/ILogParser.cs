namespace LogTracker.Common
{
    public enum LogParserErrors
    {
        OK,
        
        ConfigNotInitialized,

        RegistryNotSet, // Optional, since the log parser could simply be doing a pass to try and parse data

        MissingTimestamp,
        MissingMessage
    }
    
    public interface ILogParser
    {
        void Parse(string logFile);

        void SetRegistry(ILogRegistry registry);
        void SetConfig(LogConfig config);
    }
}
