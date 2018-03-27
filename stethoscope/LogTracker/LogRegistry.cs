using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogTracker
{
    public class LogRegistry
    {
        public LogEntry AddLog(string timestamp, string message)
        {
            DateTime time;
            if (!DateTime.TryParse(timestamp, out time))
            {
                return null;
            }
            return new LogEntry(time, message);
        }

        //TODO: special get functions - get by function, get by thread ID, get by <key>, etc.
    }
}
