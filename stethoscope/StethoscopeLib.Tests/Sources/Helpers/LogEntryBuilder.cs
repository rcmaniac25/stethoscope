using LogTracker.Common;
using LogTracker.Log;

using System;

namespace LogTracker.Tests.Helpers
{
    public enum LogEntryMsg
    {
        Source,
        Test,

        User
    }

    public enum LogEntryTime
    {
        Source,
        Test,

        Now
    }

    public class LogEntryBuilder
    {
        private LogEntry entry;

        private LogEntryBuilder(LogEntry entry)
        {
            this.entry = entry;
        }

        private static DateTime sourceDataTime = new DateTime(2018, 4, 22);
        private static DateTime testDataTime = new DateTime(2018, 4, 21);

        public static LogEntryBuilder LogEntry(LogEntryTime time = LogEntryTime.Source, LogEntryMsg msg = LogEntryMsg.Source, string userMsg = null)
        {
            DateTime timestamp;
            switch (time)
            {
                case LogEntryTime.Source:
                    timestamp = sourceDataTime;
                    break;
                case LogEntryTime.Test:
                    timestamp = testDataTime;
                    break;
                case LogEntryTime.Now:
                default:
                    timestamp = DateTime.Now;
                    break;
            }

            string logMsg;
            switch (msg)
            {
                case LogEntryMsg.Source:
                    logMsg = "source-msg";
                    break;
                case LogEntryMsg.Test:
                    logMsg = "test-msg";
                    break;
                case LogEntryMsg.User:
                default:
                    logMsg = userMsg;
                    break;
            }

            return new LogEntryBuilder(new LogEntry(timestamp, logMsg));
        }

        public LogEntryBuilder And(LogAttribute attribute, object value)
        {
            entry.AddAttribute(attribute, value);
            return this;
        }

        public LogEntry Build()
        {
            return entry;
        }
    }
}
