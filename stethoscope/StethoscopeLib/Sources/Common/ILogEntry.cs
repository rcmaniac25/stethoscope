using System;

namespace Stethoscope.Common
{
    /// <summary>
    /// An individual log entry within a log.
    /// </summary>
    public interface ILogEntry
    {
        /// <summary>
        /// Timestamp of the log entry.
        /// Depending on when the logger used, this could be when the log was written, when the log was invoked, or something else.
        /// </summary>
        DateTime Timestamp { get; }
        /// <summary>
        /// The specific log message.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// If this is a valid log entry. Valid entries must have a timestamp and message and to have been parsed successfully.
        /// </summary>
        /// <seealso cref="LogParserFailureHandling"/>
        bool IsValid { get; }

        /// <summary>
        /// Get if a specific log attribute exists.
        /// </summary>
        /// <param name="attribute">The attribute to get if it exists.</param>
        /// <returns><c>true</c> if the attribute exists. <c>false</c> if otherwise.</returns>
        bool HasAttribute(LogAttribute attribute);
        /// <summary>
        /// Get the specific log attribute.
        /// </summary>
        /// <typeparam name="T">The type of the log entry. Exception thrown if type mismatch.</typeparam>
        /// <param name="attribute">The attribute to get. Exception thrown if it doesn't exist.</param>
        /// <returns>The attribute value.</returns>
        /// <seealso cref="HasAttribute(LogAttribute)"/>
        T GetAttribute<T>(LogAttribute attribute);
    }
}
