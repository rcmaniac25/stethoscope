namespace LogTracker.Common
{
    public enum ContextConfigs
    {
        /// <summary>
        /// Log source. Must be a string. Value will be saved to <see cref="LogAttribute.LogSource"/>
        /// </summary>
        LogSource,
        /// <summary>
        /// How to handle errors while parsing a log. Must be a <see cref="LogParserFailureHandling"/>.
        /// </summary>
        FailureHandling

        //TODO: log source attribue. Ex. log file, stream URL, server name, etc.
        //      One idea to apply this without exposing some global: ILogParser.UseLogSourceContext(object source, Action<ILogParser> sourceContext). The log parser is valid only for the lofetime of the action delegate's context. All logs parsed within the context will have the log source applied to it.
        //      Would imply threads/tasks could be used... parser needs a way to be told "stop" while it's parsing. Actually... async could be useful here.
    }
}
