using LogTracker.Common;
using LogTracker.Log.Internal;

using NUnit.Framework;

namespace LogTracker.Tests.Helpers
{
    public class LogEntryTestDataBuilder : TestDataBuilder
    {
        private LogEntryTestDataBuilder(TestCaseData testData) : base(testData)
        {
        }

        private static LogEntry CreateTestLogEntry()
        {
            return LogEntryBuilder.LogEntry(LogEntryTime.Test, LogEntryMsg.Test).Build();
        }

        public static LogEntryTestDataBuilder TestAgainst(object testData)
        {
            return new LogEntryTestDataBuilder(new TestCaseData(testData, CreateTestLogEntry()));
        }

        public static LogEntryTestDataBuilder TestAgainst(LogEntryBuilder logEntryBuilder)
        {
            if (logEntryBuilder == null)
            {
                // Fix for C# liking to default "null" to this function instead of the object one
                return TestAgainst((object)logEntryBuilder);
            }
            return TestAgainst(logEntryBuilder.Build());
        }

        public static LogEntryTestDataBuilder TestAgainstLogEntry(LogEntryTime time = LogEntryTime.Source, LogEntryMsg msg = LogEntryMsg.Source, string userMsg = null)
        {
            return TestAgainst(LogEntryBuilder.LogEntry(time, msg, userMsg).Build());
        }

        public LogEntryTestDataBuilder AndHas(LogAttribute attribute, object value)
        {
            ((LogEntry)testData.OriginalArguments[1]).AddAttribute(attribute, value); // Changes both OriginalArguments and Arguments
            //((LogEntry)testData.Arguments[1]).AddAttribute(attribute, value);
            return this;
        }
    }
}
