using LogTracker.Common;
using LogTracker.Log;
using LogTracker.Tests.Helpers;

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
            Assert.That(entry.HasAttribute(LogAttribute.ThreadID), Is.False);
        }

        [Test]
        public void AttributeTypeCast()
        {
            var entry = new LogEntry(DateTime.Now, "testmsg");
            Assert.Throws<InvalidCastException>(() =>
            {
                entry.GetAttribute<int>(LogAttribute.Message);
            });
        }

        [Test]
        public void AttributeNotExist()
        {
            var entry = new LogEntry(DateTime.Now, "testmsg");
            Assert.Throws<KeyNotFoundException>(() =>
            {
                entry.GetAttribute<string>(LogAttribute.ThreadID);
            });
        }

        [Test]
        public void AddAttribute()
        {
            var entry = new LogEntry(DateTime.Now, "testmsg");
            Assert.That(entry.HasAttribute(LogAttribute.ThreadID), Is.False);

            entry.AddAttribute(LogAttribute.ThreadID, "threadid");

            Assert.That(entry.HasAttribute(LogAttribute.ThreadID), Is.True);
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
        public void ValidLog()
        {
            var entry = new LogEntry(DateTime.Now, "testmsg");
            Assert.That(entry.IsValid, Is.True);
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
            Assert.That(entry.HasAttribute(LogAttribute.Message), Is.True);
            Assert.That(entry.GetAttribute<string>(LogAttribute.Message), Is.EqualTo(msg));
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
            Assert.That(entry.HasAttribute(LogAttribute.Timestamp), Is.True);
            Assert.That(entry.GetAttribute<DateTime>(LogAttribute.Timestamp), Is.EqualTo(time));
        }

        private static readonly TestCaseData[] EqualsObjectCases =
        {
            LogEntryTestDataBuilder.TestAgainst(null).For("Equals(Object) null").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainst("notobj").For("Equals(Object) nullable type").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainst(10).For("Equals(Object) non-nullable type").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainstLogEntry().For("Equals(Object) LogEntry(diff, diff)").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainstLogEntry(msg: LogEntryMsg.Test).For("Equals(Object) LogEntry(diff, same)").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainstLogEntry(time: LogEntryTime.Test).For("Equals(Object) LogEntry(same, diff)").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainstLogEntry(LogEntryTime.Test, LogEntryMsg.Test).For("Equals(Object) LogEntry(same, same)").Which().Returns(true),
            LogEntryTestDataBuilder.TestAgainst(LogEntryBuilder.LogEntry(LogEntryTime.Test, LogEntryMsg.Test).And(LogAttribute.Module, "test")).For("Equals(Object) LogEntry(att:mod-null)").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainstLogEntry(LogEntryTime.Test, LogEntryMsg.Test).For("Equals(Object) LogEntry(att:null-mod)").AndHas(LogAttribute.Module, "test").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainst(LogEntryBuilder.LogEntry(LogEntryTime.Test, LogEntryMsg.Test).And(LogAttribute.Module, "test")).For("Equals(Object) LogEntry(att:mod-sect)").AndHas(LogAttribute.Section, "test").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainst(LogEntryBuilder.LogEntry(LogEntryTime.Test, LogEntryMsg.Test).And(LogAttribute.Module, "test")).For("Equals(Object) LogEntry(att:mod-mod)").AndHas(LogAttribute.Module, "test").Which().Returns(true)
        };

        [TestCaseSource("EqualsObjectCases")]
        public bool EqualsObject(object testData, LogEntry logEntry)
        {
            return logEntry.Equals(testData);
        }

        private static readonly TestCaseData[] EqualsLogEntryCases =
        {
            LogEntryTestDataBuilder.TestAgainst(null).For("Equals(LogEntry) null").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainstLogEntry().For("Equals(LogEntry) LogEntry(diff, diff)").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainstLogEntry(msg: LogEntryMsg.Test).For("Equals(LogEntry) LogEntry(diff, same)").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainstLogEntry(time: LogEntryTime.Test).For("Equals(LogEntry) LogEntry(same, diff)").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainstLogEntry(LogEntryTime.Test, LogEntryMsg.Test).For("Equals(LogEntry) LogEntry(same, same)").Which().Returns(true),
            LogEntryTestDataBuilder.TestAgainst(LogEntryBuilder.LogEntry(LogEntryTime.Test, LogEntryMsg.Test).And(LogAttribute.Module, "test")).For("Equals(LogEntry) LogEntry(att:mod-null)").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainstLogEntry(LogEntryTime.Test, LogEntryMsg.Test).For("Equals(LogEntry) LogEntry(att:null-mod)").AndHas(LogAttribute.Module, "test").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainst(LogEntryBuilder.LogEntry(LogEntryTime.Test, LogEntryMsg.Test).And(LogAttribute.Module, "test")).For("Equals(LogEntry) LogEntry(att:mod-sect)").AndHas(LogAttribute.Section, "test").Which().Returns(false),
            LogEntryTestDataBuilder.TestAgainst(LogEntryBuilder.LogEntry(LogEntryTime.Test, LogEntryMsg.Test).And(LogAttribute.Module, "test")).For("Equals(LogEntry) LogEntry(att:mod-mod)").AndHas(LogAttribute.Module, "test").Which().Returns(true)
        };

        [TestCaseSource("EqualsLogEntryCases")]
        public bool EqualsLogEntry(LogEntry testData, LogEntry logEntry)
        {
            return logEntry.Equals(testData);
        }

        //TODO: re-add the GetHashCode tests

        //TODO: test against failed log entries? (should a LogEntry and FailedLogEntry with the same values == true? Gut feeling is no)
    }
}
