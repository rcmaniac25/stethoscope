using System.Collections.Generic;

namespace LogTracker.Common
{
    /// <summary>
    /// A registry for all log entries.
    /// </summary>
    public interface ILogRegistry
    {
        /// <summary>
        /// Add a new log to the registry.
        /// </summary>
        /// <param name="timestamp">String representation of a timestamp.</param>
        /// <param name="message">The message that was logged for this entry.</param>
        /// <returns>The created log entry.</returns>
        ILogEntry AddLog(string timestamp, string message);
        /// <summary>
        /// Add a "failed" log to the registry. This is often a log entry that could not be parsed fully by the log parser.
        /// </summary>
        /// <returns>A failed log entry. <see cref="ILogEntry.IsValid"/> will return <c>false</c> for these entries.</returns>
        /// <remarks><see cref="GetBy(LogAttribute)"/> might not return these entries. <see cref="GetByTimetstamp"/> will only return these entries if the timestamp exists.</remarks>
        /// <seealso cref="LogParserFailureHandling"/>
        ILogEntry AddFailedLog();

        /// <summary>
        /// Add an attribute value to the log entry.
        /// </summary>
        /// <param name="entry">The entry to add an attribute to.</param>
        /// <param name="attribute">The attribute to add to.</param>
        /// <param name="value">The attribute value to add.</param>
        /// <returns><c>true</c> if the entry was added. <c>false</c> if otherwise.</returns>
        bool AddValueToLog(ILogEntry entry, LogAttribute attribute, object value);

        /// <summary>
        /// Notify the registry that the failed log has finished getting parsed. This does not indicate it has been successfully parsed, just that it is done parsing.
        /// </summary>
        /// <param name="entry">The failed log entry. Any other log entry will throw an exception.</param>
        void NotifyFailedLogParsed(ILogEntry entry);

        //TODO: switch to an enumeration of IGrouping
        /// <summary>
        /// Get logs by a specific attribute.
        /// </summary>
        /// <param name="attribute">The attribute to get entries by.</param>
        /// <returns>Logs grouped by the specified attribute.</returns>
        IDictionary<object, IEnumerable<ILogEntry>> GetBy(LogAttribute attribute);
        /// <summary>
        /// Get logs, ordered by timestamp.
        /// </summary>
        /// <returns>Enumeration of all log entries by time. Entries missing timestamps are not included.</returns>
        IEnumerable<ILogEntry> GetByTimetstamp();
    }
}
