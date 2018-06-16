using Stethoscope.Log.Internal;

using System;
using System.Reactive.Linq;

namespace Stethoscope.Common
{
    public static class LogRegistryExtensions
    {
        /// <summary>
        /// Get logs by a specific attribute.
        /// </summary>
        /// <param name="registry">Log registry</param>
        /// <param name="attribute">The attribute to get entries by.</param>
        /// <returns>Logs grouped by the specified attribute.</returns>
        public static IObservable<IGroupedObservable<object, ILogEntry>> GetBy(this ILogRegistry registry, LogAttribute attribute)
        {
            //TODO: record stat about function and attribute used
            if (attribute == LogAttribute.Timestamp)
            {
                return null;
            }
            // Special case to ensure that message and timestamp (which doesn't work here anyway) match the expected types
            return from log in registry.Logs
                   where log.HasAttribute(attribute) && (log.IsValid || !(attribute == LogAttribute.Message && !(log.GetAttribute<object>(attribute) is string)))
                   group log by log.GetAttribute<object>(attribute);
        }

        private static bool IsValidForTimestamp(this ILogEntry log)
        {
            //XXX is this still needed, or does registry.Logs do this implicitly?

            // Special case to ensure that message (which doesn't apply here anyway) and timestamp match the expected types
            // For failed logs, timestamps that haven't been sorted are ignored.
            var isInvalid = !log.IsValid &&
                   ((log is IInternalLogEntry internalLog && internalLog.HasTimestampChanged) ||
                   !log.HasAttribute(LogAttribute.Timestamp) || !(log.GetAttribute<object>(LogAttribute.Timestamp) is DateTime));

            return !isInvalid;
        }

        /// <summary>
        /// Get logs, ordered by timestamp.
        /// </summary>
        /// <param name="registry">Log registry</param>
        /// <returns>Observable of all log entries by time. Entries missing timestamps are not included.</returns>
        public static IObservable<ILogEntry> GetByTimetstamp(this ILogRegistry registry)
        {
            //TODO: record stat about function used
            return from log in registry.Logs
                   where log.IsValidForTimestamp()
                   select log;
        }

        //TODO: special get functions - get by function, get by thread ID, get by <key>, etc.?
    }
}
