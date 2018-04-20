using LogTracker.Log;

using NUnit.Framework;

using System;
using System.Collections.Generic;

namespace LogTracker.Tests
{
    [TestFixture(TestOf = typeof(LogEntry))]
    public class LogEntryTests
    {
        //XXX do we want to use TestCaseData?
        public struct TestObject
        {
            public const string SourceLogMessage = "test-logmsg";

            public object value;
            public bool result;
            public bool useTimestamp;
            public bool useMessage;

            public bool IsLogEntryTest { get; private set; }
            
            public LogEntry GetSourceLogEntry()
            {
                var msg = SourceLogMessage;
                if (useMessage)
                {
                    msg = ((LogEntry)value).Message;
                }

                var time = DateTime.Now;
                if (useMessage)
                {
                    time = ((LogEntry)value).Timestamp;
                }

                return new LogEntry(time, msg);
            }

            public override string ToString()
            {
                var valueString = value == null ? "null" : value.ToString();
                return $"Test=({valueString}), Result={result}, Copy=(time={useTimestamp}, {useMessage})";
            }

            public static TestObject CreateObjectTest(object value, bool result)
            {
                return CreateObjectTest(value, result, false, false);
            }

            public static TestObject CreateObjectTest(object value, bool result, bool useTimestamp, bool useMessage)
            {
                return new TestObject()
                {
                    value = value,
                    result = result,
                    useTimestamp = useTimestamp,
                    useMessage = useMessage,
                    IsLogEntryTest = false
                };
            }

            public static TestObject CreateLogEntryTest(LogEntry value, bool result)
            {
                return CreateLogEntryTest(value, result, false, false);
            }

            public static TestObject CreateLogEntryTest(LogEntry value, bool result, bool useTimestamp, bool useMessage)
            {
                return new TestObject()
                {
                    value = value,
                    result = result,
                    useTimestamp = useTimestamp,
                    useMessage = useMessage,
                    IsLogEntryTest = true
                };
            }
        }

        private static TestObject[] EqualsObjectCases =
        {
            TestObject.CreateObjectTest(null, false), // null
            TestObject.CreateObjectTest("notobj", false), // nullable
            TestObject.CreateObjectTest(10, false), // non-nullable
            TestObject.CreateObjectTest(new LogEntry(DateTime.Now, "logmsg"), false, false, false), // (mismatch, mismatch)
            TestObject.CreateObjectTest(new LogEntry(DateTime.Now, TestObject.SourceLogMessage), false, false, false), // (mismatch, match-org)
            TestObject.CreateObjectTest(new LogEntry(DateTime.Now, "logmsg"), false, false, true), // (mismatch, match-copy)
            TestObject.CreateObjectTest(new LogEntry(DateTime.Now, "logmsg"), false, true, false), // (match, mismatch)
            TestObject.CreateObjectTest(new LogEntry(DateTime.Now, "logmsg"), true, true, true) // (match, match)
            //TODO: setting parameter values
        };

        private static TestObject[] EqualsLogEntryCases =
        {
            TestObject.CreateLogEntryTest(null, false), // null
            TestObject.CreateLogEntryTest(new LogEntry(DateTime.Now, "logmsg"), false), // (mismatch, mismatch)
            TestObject.CreateLogEntryTest(new LogEntry(DateTime.Now, TestObject.SourceLogMessage), false), // (mismatch, match-org)
            TestObject.CreateLogEntryTest(new LogEntry(DateTime.Now, "logmsg"), false, false, true), // (mismatch, match-copy)
            TestObject.CreateLogEntryTest(new LogEntry(DateTime.Now, "logmsg"), false, true, false), // (match, mismatch)
            TestObject.CreateLogEntryTest(new LogEntry(DateTime.Now, "logmsg"), true, true, true) // (match, match)
            //TODO: setting parameter values
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
        public void EqualsObject(TestObject setup)
        {
            Assert.That(setup.IsLogEntryTest, Is.False); // Ensure correct source data is being used

            var entry = setup.GetSourceLogEntry();
            Assert.That(entry.Equals(setup.value), Is.EqualTo(setup.result));
        }

        [TestCaseSource("EqualsLogEntryCases")]
        public void EqualsLogEntry(TestObject setup)
        {
            Assert.That(setup.IsLogEntryTest, Is.True); // Ensure correct source data is being used

            var entry = setup.GetSourceLogEntry();
            Assert.That(entry.Equals((LogEntry)setup.value), Is.EqualTo(setup.result));
        }
        
        //TODO: test GetHashCode, and ToString
    }
}
