using System;
using System.Reactive.Linq;

using Stethoscope.Common;

namespace Stethoscope.Log.Internal
{
    /// <summary>
    /// Storage for log entries.
    /// </summary>
    public interface IRegistryStorage
    {
        /// <summary>
        /// Access the log entries stored in this, stored in the order of <see cref="SortAttribute"/>
        /// </summary>
        IQbservable<ILogEntry> Entries { get; }
        /// <summary>
        /// The number of logs stored within this storage.
        /// </summary>
        int Count { get; }
        /// <summary>
        /// The log attribute used to sort entries.
        /// </summary>
        LogAttribute SortAttribute { get; }

        /// <summary>
        /// Add a new log entry, sorting by <see cref="SortAttribute"/>. If the attribute does not contain the attribute, the log might not be added to the storage container. If this is the case, an empty GUID will be returned.
        /// </summary>
        /// <param name="entry">The log entry to add.</param>
        /// <returns>The <see cref="Guid"/> that internally identifies the log entry. An empty GUID means that the log was not added.</returns>
        Guid AddLogSorted(ILogEntry entry);
        /// <summary>
        /// Removes all log entries from the storage container.
        /// </summary>
        void Clear();

        //XXX: close/shutdown?
    }
}
