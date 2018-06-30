namespace Stethoscope.Common
{
    /// <summary>
    /// Factory interface to create a log parser.
    /// </summary>
    public interface ILogParserFactory
    {
        /// <summary>
        /// Create a new log parser.
        /// </summary>
        /// <param name="registry">The registy that logs will be saved to once parsed.</param>
        /// <param name="config">Configurations to apply to the log parser.</param>
        /// <returns>The created log parser.</returns>
        ILogParser Create(ILogRegistry registry, LogConfig config);
    }
}
