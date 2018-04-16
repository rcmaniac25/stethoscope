using LogTracker.Log;

using NUnit.Framework;
using System;

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

#if false
            var logs = registry.GetBy(Common.LogAttribute.Level);
            Assert.That(logs, Is.Not.Empty);
#endif
        }

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

        //TODO: AddLog (log entry contents)
        //TODO: AddLog (invalid values)
        //TODO: AddValueToLog...
        //TODO: GetBy (beyond the simple test we did) XXX also, maybe getting a dictionary of enumerations would be better then an array, that way GetBy(Message) is possible without writing a massive number of logs out. Don't allow by timestamp, that's for... (see below)
        //TODO: should probably create an enumeration method for all logs (do I want enumeration or should it be Rx?)

        //TODO: actual implementation tests
    }
}
