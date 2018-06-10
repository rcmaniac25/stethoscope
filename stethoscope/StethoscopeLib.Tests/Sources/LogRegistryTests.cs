﻿using Stethoscope.Common;
using Stethoscope.Log;
using Stethoscope.Log.Internal;
using Stethoscope.Tests.Helpers;

using NSubstitute;

using NUnit.Framework;

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;

namespace Stethoscope.Tests
{
    [TestFixture(TestOf = typeof(LogRegistry))]
    public class LogRegistryTests
    {
        private static IEnumerable<T> TempEnumerableConvert<T>(IObservable<T> obs)
        {
            //XXX This is purely so later when this is replaced, it's easy to spot the broken elements.
            //TODO: need to update tests to use observables and NUnit constraints
            if (obs == null)
            {
                return null;
            }
            return obs.ToEnumerable();
        }

        private static IDictionary<K, IEnumerable<T>> TempDictConvertGrouped<K, T>(IObservable<IGroupedObservable<K, T>> obs)
        {
            //XXX This is purely so later when this is replaced, it's easy to spot the broken elements.
            //TODO: need to update tests to use observables and NUnit constraints
            if (obs == null)
            {
                return null;
            }
            return obs.ToDictionary(group => group.Key, group => group.ToEnumerable()).Wait();
        }

        private static LogRegistry CreateLogRegistry()
        {
            //TODO
            return new LogRegistry(null);
        }
        
        [Test]
        public void GetLogsEmpty()
        {
            var registry = CreateLogRegistry();
            var logs = TempDictConvertGrouped(registry.GetBy(LogAttribute.Level));
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void AddLog()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            var logs = TempDictConvertGrouped(registry.GetBy(LogAttribute.Message));
            Assert.That(logs, Is.Not.Empty);
        }

        [Test]
        public void AddLogCheckContents()
        {
            var logmsg = "testmsg";
            var timestamp = DateTime.Now;

            var registry = CreateLogRegistry();
            var entry = registry.AddLog(timestamp.ToString(), logmsg);
            Assert.That(entry, Is.Not.Null);

            Assert.That(entry.IsValid, Is.True);

            Assert.That(entry.Message, Is.EqualTo(logmsg));
            Assert.That(entry.Timestamp, Is.EqualTo(timestamp).Within(1).Seconds);
        }

        [Test]
        public void AddLogInvalidMessage()
        {
            var registry = CreateLogRegistry();
            Assert.Throws<ArgumentNullException>(() =>
            {
                registry.AddLog(DateTime.Now.ToString(), null);
            });
        }

        [Test]
        public void AddLogInvalidTimestamp()
        {
            var registry = CreateLogRegistry();
            Assert.Throws<ArgumentException>(() =>
            {
                registry.AddLog("cookie", "testmsg");
            });
        }

        [Test]
        public void AddFailedLog()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddFailedLog();
            Assert.That(entry, Is.Not.Null);

            Assert.That(entry.IsValid, Is.False);
        }

        [Test]
        public void NotifyFailedLogParsed()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddFailedLog();
            Assert.That(entry, Is.Not.Null);

            registry.NotifyFailedLogParsed(entry);
        }

        [Test]
        public void NotifyFailedLogParsedNull()
        {
            var registry = CreateLogRegistry();
            Assert.Throws<ArgumentNullException>(() =>
            {
                registry.NotifyFailedLogParsed(null);
            });
        }

        [Test]
        public void NotifyFailedLogParsedWithGoodLog()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            Assert.Throws<ArgumentException>(() =>
            {
                registry.NotifyFailedLogParsed(entry);
            });
        }

        [Test]
        public void AddValueToLog()
        {
            var registry = CreateLogRegistry();
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
            var registry = CreateLogRegistry();
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
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            Assert.That(entry.HasAttribute(LogAttribute.ThreadID), Is.False);

            var added = registry.AddValueToLog(entry, LogAttribute.ThreadID, null);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToLogNullEntry()
        {
            var registry = CreateLogRegistry();
            var added = registry.AddValueToLog(null, LogAttribute.ThreadID, 1234);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToLogMessage()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);
            
            var added = registry.AddValueToLog(entry, LogAttribute.Message, "new message");

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToFailedLogMessage()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddFailedLog();
            Assert.That(entry, Is.Not.Null);

            var added = registry.AddValueToLog(entry, LogAttribute.Message, "new message");

            Assert.That(added, Is.True);
        }

        [Test]
        public void AddValueToLogTimestamp()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);
            
            var added = registry.AddValueToLog(entry, LogAttribute.Timestamp, DateTime.Now);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToFailedLogTimestamp()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddFailedLog();
            Assert.That(entry, Is.Not.Null);

            var added = registry.AddValueToLog(entry, LogAttribute.Timestamp, DateTime.Now);

            Assert.That(added, Is.True);
        }

        [Test]
        public void AddValueToLogInvalidLogEntry()
        {
            var registry = CreateLogRegistry();
            var entry = Substitute.For<ILogEntry>();

            var added = registry.AddValueToLog(entry, LogAttribute.ThreadID, 1234);

            Assert.That(added, Is.False);
        }

        [Test]
        public void AddValueToLogValidLogEntry()
        {
            var registry = CreateLogRegistry();
            var entry = Substitute.For<IInternalLogEntry>();

            var added = registry.AddValueToLog(entry, LogAttribute.ThreadID, 1234);

            Assert.That(added, Is.True);
            entry.Received().AddAttribute(LogAttribute.ThreadID, 1234);
        }

        [Test]
        public void GetByLogsWithoutAttributes()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            var logs = TempDictConvertGrouped(registry.GetBy(LogAttribute.Level));
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void GetByLogsSomeAttributesWithUniqueValues()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg1");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "cookie");

            entry = registry.AddLog(DateTime.Now.ToString(), "testmsg2");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "brownie");

            entry = registry.AddLog(DateTime.Now.ToString(), "testmsg3");
            Assert.That(entry, Is.Not.Null);

            var logs = TempDictConvertGrouped(registry.GetBy(LogAttribute.Level));
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.Keys, Is.SubsetOf(new string[] { "cookie", "brownie" }).And.Exactly(2).Items);
            Assert.That(Util.ConcatMany(logs.Values).Select(log => log.Message), Is.SubsetOf(new string[] { "testmsg1", "testmsg2" }));
        }

        [Test]
        public void GetByLogsSomeAttributesWithDuplicateValues()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg1");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "cookie");

            entry = registry.AddLog(DateTime.Now.ToString(), "testmsg2");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "cookie");

            entry = registry.AddLog(DateTime.Now.ToString(), "testmsg3");
            Assert.That(entry, Is.Not.Null);

            var logs = TempDictConvertGrouped(registry.GetBy(LogAttribute.Level));
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.Keys, Has.Member("cookie").And.Exactly(1).Items);
            Assert.That(Util.ConcatMany(logs.Values).Select(log => log.Message), Is.SubsetOf(new string[] { "testmsg1", "testmsg2" }));
        }

        [Test]
        public void GetByLogsAllAttributesWithUniqueValues()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg1");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "cookie");

            entry = registry.AddLog(DateTime.Now.ToString(), "testmsg2");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "brownie");

            entry = registry.AddLog(DateTime.Now.ToString(), "testmsg3");
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "ice cream");

            var logs = TempDictConvertGrouped(registry.GetBy(LogAttribute.Level));
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.Keys, Is.SubsetOf(new string[] { "cookie", "brownie", "ice cream" }).And.Exactly(3).Items);
            Assert.That(Util.ConcatMany(logs.Values).Select(log => log.Message), Is.SubsetOf(new string[] { "testmsg1", "testmsg2", "testmsg3" }));
        }

        [Test]
        public void GetByLogsInvalidAttribute()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg1");
            Assert.That(entry, Is.Not.Null);

            var logs = TempDictConvertGrouped(registry.GetBy(LogAttribute.Message));
            Assert.That(logs, Is.Not.Empty);

            logs = TempDictConvertGrouped(registry.GetBy(LogAttribute.Timestamp));
            Assert.That(logs, Is.Null);
        }

        [Test]
        public void GetByLogsWithFailedLogs()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "msg1");
            Assert.That(entry, Is.Not.Null);

            entry = registry.AddFailedLog();
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Message, "msg2");

            registry.NotifyFailedLogParsed(entry);

            var logs = TempDictConvertGrouped(registry.GetBy(LogAttribute.Message));
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.Keys, Is.SubsetOf(new string[] { "msg1", "msg2" }).And.Exactly(2).Items);
        }

        [Test]
        public void GetByLogsWithFailedLogsMissingNotify()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "msg1");
            Assert.That(entry, Is.Not.Null);

            entry = registry.AddFailedLog();
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Message, "msg2");

            var logs = TempDictConvertGrouped(registry.GetBy(LogAttribute.Message));
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.Keys, Is.SubsetOf(new string[] { "msg1", "msg2" }).And.Exactly(2).Items);
        }

        [Test]
        public void GetByLogsWithFailedLogsWithoutAttribute()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "msg1");
            Assert.That(entry, Is.Not.Null);

            entry = registry.AddFailedLog();
            Assert.That(entry, Is.Not.Null);

            registry.AddValueToLog(entry, LogAttribute.Level, "something");

            var logs = TempDictConvertGrouped(registry.GetBy(LogAttribute.Message));
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.Keys, Is.SubsetOf(new string[] { "msg1" }).And.Exactly(1).Items);
        }

        [Test]
        public void GetByTimetstampEmpty()
        {
            var registry = CreateLogRegistry();
            Assert.That(TempEnumerableConvert(registry.GetByTimetstamp()), Is.Empty);
        }

        [Test]
        public void GetByTimetstamp()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            var logs = TempEnumerableConvert(registry.GetByTimetstamp());
            Assert.That(logs, Is.Not.Empty);
            
            var entryFromEnumeration = logs.Last();
            Assert.That(entryFromEnumeration, Is.EqualTo(entry));
        }

        [Test]
        public void GetByTimetstampOrdered()
        {
            var time = DateTime.Now;

            var registry = CreateLogRegistry();
            var entry = registry.AddLog(time.ToString(), "testmsg1");
            Assert.That(registry.AddValueToLog(entry, LogAttribute.Level, 1), Is.True);

            entry = registry.AddLog(time.AddSeconds(1).ToString(), "testmsg2");
            Assert.That(registry.AddValueToLog(entry, LogAttribute.Level, 2), Is.True);

            var logs = TempEnumerableConvert(registry.GetByTimetstamp());
            Assert.That(logs, Is.Not.Empty);
            
            Assert.That(logs.First().GetAttribute<int>(LogAttribute.Level), Is.EqualTo(1));
            Assert.That(logs.Last().GetAttribute<int>(LogAttribute.Level), Is.EqualTo(2));
        }

        [Test]
        public void GetByTimetstampOrderedReversed()
        {
            var time = DateTime.Now;

            var registry = CreateLogRegistry();
            var entry = registry.AddLog(time.AddSeconds(1).ToString(), "testmsg2");
            Assert.That(registry.AddValueToLog(entry, LogAttribute.Level, 2), Is.True);

            entry = registry.AddLog(time.ToString(), "testmsg1");
            Assert.That(registry.AddValueToLog(entry, LogAttribute.Level, 1), Is.True);

            var logs = TempEnumerableConvert(registry.GetByTimetstamp());
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.First().GetAttribute<int>(LogAttribute.Level), Is.EqualTo(1));
            Assert.That(logs.Last().GetAttribute<int>(LogAttribute.Level), Is.EqualTo(2));
        }

        [Test]
        public void GetByTimetstampWithFailedLogs()
        {
            var time = DateTime.Now;

            var registry = CreateLogRegistry();
            var entry = registry.AddLog(time.AddSeconds(1).ToString(), "msg1");

            entry = registry.AddFailedLog();
            registry.AddValueToLog(entry, LogAttribute.Timestamp, time);
            registry.AddValueToLog(entry, LogAttribute.Message, "msg2");
            registry.NotifyFailedLogParsed(entry);

            var logs = TempEnumerableConvert(registry.GetByTimetstamp());
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.Select(log => log.Message), Is.SubsetOf(new string[] { "msg2", "msg1" }).And.Exactly(2).Items);
        }

        [Test]
        public void GetByTimetstampWithFailedLogsMissingNotify()
        {
            var time = DateTime.Now;

            var registry = CreateLogRegistry();
            var entry = registry.AddLog(time.AddSeconds(1).ToString(), "msg1");

            entry = registry.AddFailedLog();
            registry.AddValueToLog(entry, LogAttribute.Timestamp, time);
            registry.AddValueToLog(entry, LogAttribute.Message, "msg2");

            var logs = TempEnumerableConvert(registry.GetByTimetstamp());
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs.Select(log => log.Message), Is.SubsetOf(new string[] { "msg1" }).And.Exactly(1).Items);
        }

        [Test]
        public void GetByTimetstampWithFailedLogsWithoutAttribute()
        {
            var time = DateTime.Now;

            var registry = CreateLogRegistry();
            var entry = registry.AddLog(time.AddSeconds(1).ToString(), "msg1");

            entry = registry.AddFailedLog();
            registry.AddValueToLog(entry, LogAttribute.Message, "msg2");

            var logs = TempEnumerableConvert(registry.GetByTimetstamp());
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs, Has.Exactly(1).Items);
        }

        [Test]
        public void GetByTimetstampWithFailedLogsInvalidTimestamp()
        {
            var time = DateTime.Now;

            var registry = CreateLogRegistry();
            var entry = registry.AddLog(time.AddSeconds(1).ToString(), "msg1");

            entry = registry.AddFailedLog();
            registry.AddValueToLog(entry, LogAttribute.Timestamp, "not-timestamp");

            var logs = TempEnumerableConvert(registry.GetByTimetstamp());
            Assert.That(logs, Is.Not.Empty);

            Assert.That(logs, Has.Exactly(1).Items);
        }

        [Test]
        public void Clear()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            var logs = TempEnumerableConvert(registry.GetByTimetstamp());
            Assert.That(logs, Is.Not.Empty);

            registry.Clear();

            logs = TempEnumerableConvert(registry.GetByTimetstamp());
            Assert.That(logs, Is.Empty);
        }

        [Test]
        public void LogCountEmpty()
        {
            var registry = CreateLogRegistry();
            Assert.That(registry.LogCount, Is.Zero);
        }

        [Test]
        public void LogCount()
        {
            var registry = CreateLogRegistry();
            var entry = registry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            Assert.That(registry.LogCount, Is.EqualTo(1));
        }
        
        [Test]
        public void LogCountMixed()
        {
            var time = DateTime.Now;

            var registry = CreateLogRegistry();
            var entry = registry.AddLog(time.AddSeconds(1).ToString(), "msg1");

            entry = registry.AddFailedLog();
            registry.AddValueToLog(entry, LogAttribute.Timestamp, time);
            registry.AddValueToLog(entry, LogAttribute.Message, "msg2");
            registry.NotifyFailedLogParsed(entry);

            Assert.That(registry.LogCount, Is.EqualTo(2));
        }

        [Test]
        public void LogCountMixedMissingNotify()
        {
            var time = DateTime.Now;

            var registry = CreateLogRegistry();
            var entry = registry.AddLog(time.AddSeconds(1).ToString(), "msg1");

            entry = registry.AddFailedLog();
            registry.AddValueToLog(entry, LogAttribute.Timestamp, time);
            registry.AddValueToLog(entry, LogAttribute.Message, "msg2");

            Assert.That(registry.LogCount, Is.EqualTo(2));
        }

        [Test]
        public void LogCountMixedEmptyFailed()
        {
            var time = DateTime.Now;

            var registry = CreateLogRegistry();
            var entry = registry.AddLog(time.AddSeconds(1).ToString(), "msg1");

            entry = registry.AddFailedLog();

            Assert.That(registry.LogCount, Is.EqualTo(2));

            registry.NotifyFailedLogParsed(entry);

            Assert.That(registry.LogCount, Is.EqualTo(1));
        }
    }
}
