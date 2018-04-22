﻿using LogTracker.Common;
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
        private static TestCaseData[] EqualsObjectCases =
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

        private static TestCaseData[] EqualsLogEntryCases =
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

        [TestCaseSource("EqualsObjectCases")]
        public bool EqualsObject(object testData, LogEntry logEntry)
        {
            return logEntry.Equals(testData);
        }

        [TestCaseSource("EqualsLogEntryCases")]
        public bool EqualsLogEntry(LogEntry testData, LogEntry logEntry)
        {
            return logEntry.Equals(testData);
        }
        
        //TODO: test GetHashCode, and ToString
    }
}
