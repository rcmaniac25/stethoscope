using LogTracker.Log;

using NUnit.Framework;

using System;

namespace LogTracker.Tests.Sources
{
    [TestFixture(TestOf = typeof(LogEntry))]
    public class LogEntryTests
    {
        [Test]
        public void PropertyNotSet()
        {
            var entry = new LogEntry(DateTime.Now, "testmsg");
            Assert.That(entry.HasAttribute(Common.LogAttribute.ThreadID), Is.False);
        }

        //TODO: invalid type cast
        //TODO: get attribute, value doesn't exist
        //TODO: add attribute

        [Test]
        public void LogMessageProperty()
        {
            var msg = "testmsg";
            var entry = new LogEntry(DateTime.Now, msg);
            Assert.That(entry.Message, Is.EqualTo(msg));
        }

        [Test]
        public void LogMessageAttribute()
        {
            var msg = "testmsg";
            var entry = new LogEntry(DateTime.Now, msg);
            Assert.That(entry.HasAttribute(Common.LogAttribute.Message), Is.True);
            Assert.That(entry.GetAttribute<string>(Common.LogAttribute.Message), Is.EqualTo(msg));
        }

        [Test]
        public void TimestampProperty()
        {
            var time = DateTime.Now;
            var entry = new LogEntry(time, "testmsg");
            Assert.That(entry.Timestamp, Is.EqualTo(time));
        }

        [Test]
        public void TimestampAttribute()
        {
            var time = DateTime.Now;
            var entry = new LogEntry(time, "testmsg");
            Assert.That(entry.HasAttribute(Common.LogAttribute.Timestamp), Is.True);
            Assert.That(entry.GetAttribute<DateTime>(Common.LogAttribute.Timestamp), Is.EqualTo(time));
        }
    }
}
