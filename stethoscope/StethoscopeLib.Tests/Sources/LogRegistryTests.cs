using LogTracker.Common;
using LogTracker.Log;
using LogTracker.Log.Internal;
using LogTracker.Tests.Helpers;

using NSubstitute;

using NUnit.Framework;

using System;
using System.Linq;

namespace LogTracker.Tests
{
    [TestFixture(TestOf = typeof(LogRegistry))]
    public class LogRegistryTests
    {
        [Test]
        public void GetLogsEmpty()
        {
            var registry = new LogRegistry();
            var logs = registry.GetBy(LogAttribute.Level);
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void AddLog()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);
            
            var logs = registry.GetBy(LogAttribute.Message);
            Assert.That(logs, Is.Not.Empty);
        }

        [Test]
        public void AddLogCheckContents()
        {
            var logmsg = "testmsg";
            var timestamp = DateTime.Now;

            var registry = new LogRegistry();
            var entry = registry.AddLog(timestamp.ToString(), logmsg);
            Assert.That(entry, Is.Not.Null);

            Assert.That(entry.IsValid, Is.True);

            Assert.That(entry.Message, Is.EqualTo(logmsg));
            Assert.That(entry.Timestamp, Is.EqualTo(timestamp).Within(1).Seconds);
        }

        [Test]
        public void AddLogInvalidMessage()
        {
            var registry = new LogRegistry();
            Assert.Throws<ArgumentNullException>(() =>
            {
                registry.AddLog(DateTime.Now.ToString(), null);
            });
        }

        [Test]
        public void AddLogInvalidTimestamp()
        {
            var registry = new LogRegistry();
            Assert.Throws<ArgumentException>(() =>
            {
                registry.AddLog("cookie", "testmsg");
            });
        }

        [Test]
        public void AddFailedLog()
        {
            var registry = new LogRegistry();
            var entry = registry.AddFailedLog();
            Assert.That(entry, Is.Not.Null);

            Assert.That(entry.IsValid, Is.False);
        }

        [Test]
        public void AddValueToLog()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            Assert.That(entry.HasAttribute(LogAttribute.ThreadID), Is.False);

            var threadId = 1234;
            var added = registry.AddValueToLog(entry, LogAttribute.ThreadID, threadId);

            Assert.That(added, Is.True);
            Assert.That(entry.HasAttribute(LogAttribute.ThreadID), Is.True);

            Assert.That(entry.GetAttribute<int>(LogAttribute.ThreadID), Is.EqualTo(threadId));
        }

        [Test]
        public void AddValueToLogAgain()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            Assert.That(entry.HasAttribute(LogAttribute.ThreadID), Is.False);
            
            var added = registry.AddValueToLog(entry, LogAttribute.ThreadID, 1234);

            Assert.That(added, Is.True);

            added = registry.AddValueToLog(entry, LogAttribute.ThreadID, 4321);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToLogNullValue()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            Assert.That(entry.HasAttribute(LogAttribute.ThreadID), Is.False);

            var added = registry.AddValueToLog(entry, LogAttribute.ThreadID, null);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToLogNullEntry()
        {
            var registry = new LogRegistry();
            var added = registry.AddValueToLog(null, LogAttribute.ThreadID, 1234);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToLogMessage()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);
            
            var added = registry.AddValueToLog(entry, LogAttribute.Message, "new message");

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToFailedLogMessage()
        {
            var registry = new LogRegistry();
            var entry = registry.AddFailedLog();
            Assert.That(entry, Is.Not.Null);

            var added = registry.AddValueToLog(entry, LogAttribute.Message, "new message");

            Assert.That(added, Is.True);
        }

        [Test]
        public void AddValueToLogTimestamp()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);
            
            var added = registry.AddValueToLog(entry, LogAttribute.Timestamp, DateTime.Now);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToFailedLogTimestamp()
        {
            var registry = new LogRegistry();
            var entry = registry.AddFailedLog();
            Assert.That(entry, Is.Not.Null);

            var added = registry.AddValueToLog(entry, LogAttribute.Timestamp, DateTime.Now);

            Assert.That(added, Is.True);
        }

        [Test]
        public void AddValueToLogInvalidLogEntry()
        {
            var registry = new LogRegistry();
            var entry = Substitute.For<ILogEntry>();

            var added = registry.AddValueToLog(entry, LogAttribute.ThreadID, 1234);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToLogValidLogEntry()
        {
            var registry = new LogRegistry();
            var entry = Substitute.For<IMutableLogEntry>();

            var added = registry.AddValueToLog(entry, LogAttribute.ThreadID, 1234);

            Assert.That(added, Is.True);
            entry.Received().AddAttribute(LogAttribute.ThreadID, 1234);
        }

        [Test]
        public void GetByLogsWithoutAttributes()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            var logs = registry.GetBy(LogAttribute.Level);
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void GetByLogsSomeAttributesWithUniqueValues()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg1");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "cookie");

            entry = registry.AddLog(DateTime.Now.ToString(), "testmsg2");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "brownie");

            entry = registry.AddLog(DateTime.Now.ToString(), "testmsg3");
            Assert.That(entry, Is.Not.Null);

            var logs = registry.GetBy(LogAttribute.Level);
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.Keys, Is.SubsetOf(new string[] { "cookie", "brownie" }).And.Exactly(2).Items);
            Assert.That(Util.ConcatMany(logs.Values).Select(log => log.Message), Is.SubsetOf(new string[] { "testmsg1", "testmsg2" }));
        }

        [Test]
        public void GetByLogsSomeAttributesWithDuplicateValues()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg1");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "cookie");

            entry = registry.AddLog(DateTime.Now.ToString(), "testmsg2");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "cookie");

            entry = registry.AddLog(DateTime.Now.ToString(), "testmsg3");
            Assert.That(entry, Is.Not.Null);

            var logs = registry.GetBy(LogAttribute.Level);
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.Keys, Has.Member("cookie").And.Exactly(1).Items);
            Assert.That(Util.ConcatMany(logs.Values).Select(log => log.Message), Is.SubsetOf(new string[] { "testmsg1", "testmsg2" }));
        }

        [Test]
        public void GetByLogsAllAttributesWithUniqueValues()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg1");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "cookie");

            entry = registry.AddLog(DateTime.Now.ToString(), "testmsg2");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "brownie");

            entry = registry.AddLog(DateTime.Now.ToString(), "testmsg3");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "ice cream");

            var logs = registry.GetBy(LogAttribute.Level);
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.Keys, Is.SubsetOf(new string[] { "cookie", "brownie", "ice cream" }).And.Exactly(3).Items);
            Assert.That(Util.ConcatMany(logs.Values).Select(log => log.Message), Is.SubsetOf(new string[] { "testmsg1", "testmsg2", "testmsg3" }));
        }

        [Test]
        public void GetByLogsInvalidAttribute()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg1");
            Assert.That(entry, Is.Not.Null);

            var logs = registry.GetBy(LogAttribute.Message);
            Assert.That(logs, Is.Not.Empty);

            logs = registry.GetBy(LogAttribute.Timestamp);
            Assert.That(logs, Is.Null);
        }

        [Test]
        public void GetByLogsWithFailedLogs()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "msg1");
            Assert.That(entry, Is.Not.Null);

            entry = registry.AddFailedLog();
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Message, "msg2");

            var logs = registry.GetBy(LogAttribute.Message);
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.Keys, Is.SubsetOf(new string[] { "msg1", "msg2" }).And.Exactly(2).Items);
        }

        [Test]
        public void GetByLogsWithFailedLogsWithoutAttribute()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "msg1");
            Assert.That(entry, Is.Not.Null);

            entry = registry.AddFailedLog();
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "something");

            var logs = registry.GetBy(LogAttribute.Message);
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.Keys, Is.SubsetOf(new string[] { "msg1" }).And.Exactly(1).Items);
        }

        [Test]
        public void GetByTimetstampEmpty()
        {
            var registry = new LogRegistry();
            Assert.That(registry.GetByTimetstamp(), Is.Empty);
        }

        [Test]
        public void GetByTimetstamp()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            var logs = registry.GetByTimetstamp();
            Assert.That(logs, Is.Not.Empty);

            var entryFromEnumeration = logs.Last();
            Assert.That(entryFromEnumeration, Is.EqualTo(entry));
        }

        [Test]
        public void GetByTimetstampOrdered()
        {
            var time = DateTime.Now;

            var registry = new LogRegistry();
            var entry = registry.AddLog(time.ToString(), "testmsg1");
            Assert.That(registry.AddValueToLog(entry, LogAttribute.Level, 1), Is.True);

            entry = registry.AddLog(time.AddSeconds(1).ToString(), "testmsg2");
            Assert.That(registry.AddValueToLog(entry, LogAttribute.Level, 2), Is.True);

            var logs = registry.GetByTimetstamp();
            Assert.That(logs, Is.Not.Empty);
            
            Assert.That(logs.First().GetAttribute<int>(LogAttribute.Level), Is.EqualTo(1));
            Assert.That(logs.Last().GetAttribute<int>(LogAttribute.Level), Is.EqualTo(2));
        }

        [Test]
        public void GetByTimetstampOrderedReversed()
        {
            var time = DateTime.Now;

            var registry = new LogRegistry();
            var entry = registry.AddLog(time.AddSeconds(1).ToString(), "testmsg2");
            Assert.That(registry.AddValueToLog(entry, LogAttribute.Level, 2), Is.True);

            entry = registry.AddLog(time.ToString(), "testmsg1");
            Assert.That(registry.AddValueToLog(entry, LogAttribute.Level, 1), Is.True);

            var logs = registry.GetByTimetstamp();
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.First().GetAttribute<int>(LogAttribute.Level), Is.EqualTo(1));
            Assert.That(logs.Last().GetAttribute<int>(LogAttribute.Level), Is.EqualTo(2));
        }

        [Test]
        public void GetByTimetstampWithFailedLogs()
        {
            var time = DateTime.Now;

            var registry = new LogRegistry();
            var entry = registry.AddLog(time.AddSeconds(1).ToString(), "msg1");

            entry = registry.AddFailedLog();
            registry.AddValueToLog(entry, LogAttribute.Timestamp, time);

            var logs = registry.GetByTimetstamp();
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs, Has.Exactly(2).Items);
        }

        [Test]
        public void GetByTimetstampWithFailedLogsWithoutAttribute()
        {
            var time = DateTime.Now;

            var registry = new LogRegistry();
            var entry = registry.AddLog(time.AddSeconds(1).ToString(), "msg1");

            entry = registry.AddFailedLog();
            registry.AddValueToLog(entry, LogAttribute.Message, "msg2");

            var logs = registry.GetByTimetstamp();
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs, Has.Exactly(1).Items);
        }

        [Test]
        public void GetByTimetstampWithFailedLogsInvalidTimestamp()
        {
            var time = DateTime.Now;

            var registry = new LogRegistry();
            var entry = registry.AddLog(time.AddSeconds(1).ToString(), "msg1");

            entry = registry.AddFailedLog();
            registry.AddValueToLog(entry, LogAttribute.Timestamp, "not-timestamp");

            var logs = registry.GetByTimetstamp();
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs, Has.Exactly(1).Items);
        }

        [Test]
        public void Clear()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            var logs = registry.GetByTimetstamp();
            Assert.That(logs, Is.Not.Empty);

            registry.Clear();

            logs = registry.GetByTimetstamp();
            Assert.That(logs, Is.Empty);
        }
    }
}
