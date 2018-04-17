using LogTracker.Common;
using LogTracker.Log;
using LogTracker.Log.Internal;

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
            var logs = registry.GetBy(Common.LogAttribute.Level);
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void AddLog()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);
            
            var logs = registry.GetBy(Common.LogAttribute.Message);
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
        public void AddValueToLog()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            Assert.That(entry.HasAttribute(Common.LogAttribute.ThreadID), Is.False);

            var threadId = 1234;
            var added = registry.AddValueToLog(entry, Common.LogAttribute.ThreadID, threadId);

            Assert.That(added, Is.True);
            Assert.That(entry.HasAttribute(Common.LogAttribute.ThreadID), Is.True);

            Assert.That(entry.GetAttribute<int>(Common.LogAttribute.ThreadID), Is.EqualTo(threadId));
        }

        [Test]
        public void AddValueToLogAgain()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            Assert.That(entry.HasAttribute(Common.LogAttribute.ThreadID), Is.False);
            
            var added = registry.AddValueToLog(entry, Common.LogAttribute.ThreadID, 1234);

            Assert.That(added, Is.True);

            added = registry.AddValueToLog(entry, Common.LogAttribute.ThreadID, 4321);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToLogNullValue()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            Assert.That(entry.HasAttribute(Common.LogAttribute.ThreadID), Is.False);

            var added = registry.AddValueToLog(entry, Common.LogAttribute.ThreadID, null);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToLogNullEntry()
        {
            var registry = new LogRegistry();
            var added = registry.AddValueToLog(null, Common.LogAttribute.ThreadID, 1234);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToLogMessage()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);
            
            var added = registry.AddValueToLog(entry, Common.LogAttribute.Message, "new message");

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToLogTimestamp()
        {
            var registry = new LogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);
            
            var added = registry.AddValueToLog(entry, Common.LogAttribute.Timestamp, DateTime.Now);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToLogInvalidLogEntry()
        {
            var registry = new LogRegistry();
            var entry = Substitute.For<ILogEntry>();

            var added = registry.AddValueToLog(entry, Common.LogAttribute.ThreadID, 1234);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToLogValidLogEntry()
        {
            var registry = new LogRegistry();
            var entry = Substitute.For<IMutableLogEntry>();

            var added = registry.AddValueToLog(entry, Common.LogAttribute.ThreadID, 1234);

            Assert.That(added, Is.True);
            entry.Received().AddAttribute(LogAttribute.ThreadID, 1234);
        }

        //TODO: GetBy (beyond the simple test we did)

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
            Assert.That(entryFromEnumeration, Is.EqualTo(entry)); //TODO: need equals (and probably ToString and GetHashCode) functions
        }

        //TODO: check that enumeration is ordered

#if false
        [Test]
        public void AddLogAndTestContent()
        {
            var timestamp = DateTime.Now;
            var msg = "testmsg";

            var registry = new LogRegistry();
            var entry = registry.AddLog(timestamp.ToString(), msg);
            Assert.That(entry, Is.Not.Null);

            Assert.That(entry.Message, Is.EqualTo(msg));
            Assert.That(entry.Timestamp, Is.EqualTo(timestamp));

            var logs = registry.GetBy(Common.LogAttribute.Message);
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.ContainsKey(msg), Is.True);
            Assert.That(logs[msg], Is.EqualTo(entry));
        }
#endif

        //TODO: actual implementation tests
    }
}
