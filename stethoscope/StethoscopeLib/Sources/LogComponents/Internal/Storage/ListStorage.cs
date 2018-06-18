using Stethoscope.Common;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Stethoscope.Log.Internal.Storage
{
    public class ListStorage : IRegistryStorage
    {
        private class IlogEntryComparer : IComparer<ILogEntry>
        {
            private DateTime? GetTimestamp(ILogEntry entry)
            {
                if (entry.IsValid)
                {
                    return entry.GetAttribute<DateTime>(LogAttribute.Timestamp);
                }

                if (entry.HasAttribute(LogAttribute.Timestamp))
                {
                    var timestamp = entry.GetAttribute<object>(LogAttribute.Timestamp);
                    if (timestamp is DateTime ts)
                    {
                        return ts;
                    }
                }

                return null;
            }

            public int Compare(ILogEntry x, ILogEntry y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }
                else if (x == null)
                {
                    return -1;
                }
                else if (y == null)
                {
                    return 1;
                }

                var xt = GetTimestamp(x);
                var yt = GetTimestamp(y);

                if (xt.HasValue && yt.HasValue)
                {
                    return xt.Value.CompareTo(yt.Value);
                }
                else if (xt.HasValue)
                {
                    return 1;
                }
                else if (yt.HasValue)
                {
                    return -1;
                }
                return 0;
            }
        }

        private List<ILogEntry> logs = new List<ILogEntry>();
        private readonly IlogEntryComparer logEntryComparer = new IlogEntryComparer();

        public IQbservable<ILogEntry> Entries => logs.ToObservable().AsQbservable(); //XXX This won't work with "Insert" (or "Add" for that instance). This is basically saying "foreach(var log in logs) { OnNext(log); }" and enumerations don't like iteration + changes

        public int Count
        {
            get
            {
                lock (logs)
                {
                    return logs.Count;
                }
            }
        }

        //XXX this should probably be set-able, but not for now
        public LogAttribute SortAttribute => LogAttribute.Timestamp;

        public Guid AddLogSorted(ILogEntry entry)
        {
            if (!(entry is IInternalLogEntry))
            {
                //TODO: record stat about failure
                //XXX
                throw new ArgumentException("Log type is not supported (right now)", nameof(entry));
            }
            //TODO: record stat about function used
            lock (logs)
            {
                //XXX need to make sure this will continue to work

                // From https://stackoverflow.com/a/22801345/492347
                if (logs.Count == 0 || logEntryComparer.Compare(logs[logs.Count - 1], entry) <= 0)
                {
                    logs.Add(entry);
                }
                else if (logEntryComparer.Compare(logs[0], entry) >= 0)
                {
                    logs.Insert(0, entry);
                }
                else
                {
                    var index = logs.BinarySearch(entry, logEntryComparer);
                    if (index < 0)
                    {
                        index = ~index;
                    }
                    logs.Insert(index, entry);
                }
                //TODO: record stat about total count
            }
            return ((IInternalLogEntry)entry).ID;
        }

        public void Clear()
        {
            //TODO: record stat about function used
            lock (logs)
            {
                //XXX
                logs.Clear();
            }
        }
    }
}
