namespace LogTracker.Common
{
    /// <summary>
    /// Available attributes for a log
    /// </summary>
    public enum LogAttribute
    {
        /// <summary>
        /// Log timestamp
        /// </summary>
        Timestamp,
        /// <summary>
        /// Log message
        /// </summary>
        Message,

        /// <summary>
        /// Thread ID (logger system dependent)
        /// </summary>
        ThreadID,
        /// <summary>
        /// Source file for log
        /// </summary>
        SourceFile,
        /// <summary>
        /// Source function for log
        /// </summary>
        Function,
        /// <summary>
        /// Source file line for log
        /// </summary>
        SourceLine,
        /// <summary>
        /// What level (ex. 0-9, with 9 being most verbose and 0 being most critical) is the log
        /// </summary>
        Level,
        /// <summary>
        /// What sequence within the original log file, was the log at
        /// </summary>
        SequenceNumber,
        /// <summary>
        /// Module / library / executable the log came from
        /// </summary>
        Module,
        /// <summary>
        /// What type of log was produced (ex. error, warning, info)
        /// </summary>
        Type,
        /// <summary>
        /// What "code group", within a module, produced the log. Ex. Module=VideoGame, Section=Audio
        /// </summary>
        Section,
        /// <summary>
        /// Identification information for a trace that exists for a short term (Ex. an ID used within a function call)
        /// </summary>
        TraceID,
        /// <summary>
        /// Identification information for a trace that exists for a long term (Ex. unique IDs for an instance of a class)
        /// </summary>
        Context

        //TODO: log source attribue. Ex. log file, stream URL, server name, etc.
        //      One idea to apply this without exposing some global: ILogParser.UseLogSourceContext(object source, Action<ILogParser> sourceContext). The log parser is valid only for the lofetime of the action delegate's context. All logs parsed within the context will have the log source applied to it.
        //      Would imply threads/tasks could be used... parser needs a way to be told "stop" while it's parsing. Actually... async could be useful here.
    }
}
