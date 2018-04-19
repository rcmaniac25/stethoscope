using LogTracker.Log;

using NUnit.Framework;

using System;
using System.Collections.Generic;

namespace LogTracker.Tests
{
    [TestFixture(TestOf = typeof(LogEntry))]
    public class LogEntryTests
    {
        [Test]
        public void AttributeNotSet()
        {
            var entry = new LogEntry(DateTime.Now, "testmsg");
            Assert.That(entry.HasAttribute(Common.LogAttribute.ThreadID), Is.False);
        }

        [Test]
        public void AttributeTypeCast()
        {
            var entry = new LogEntry(DateTime.Now, "testmsg");
            Assert.Throws<InvalidCastException>(() =>
            {
                entry.GetAttribute<int>(Common.LogAttribute.Message);
            });
        }

        [Test]
        public void AttributeNotExist()
        {
            var entry = new LogEntry(DateTime.Now, "testmsg");
            Assert.Throws<KeyNotFoundException>(() =>
            {
                entry.GetAttribute<string>(Common.LogAttribute.ThreadID);
            });
        }

        [Test]
        public void AddAttribute()
        {
            var entry = new LogEntry(DateTime.Now, "testmsg");
            Assert.That(entry.HasAttribute(Common.LogAttribute.ThreadID), Is.False);

            entry.AddAttribute(Common.LogAttribute.ThreadID, "threadid");

            Assert.That(entry.HasAttribute(Common.LogAttribute.ThreadID), Is.True);
        }

        [Test]
        public void InvalidMessage()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new LogEntry(DateTime.Now, null);
            });
        }

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

        //TODO: test Equals, GetHashCode, and ToString
    }
}
