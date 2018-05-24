using LogTracker.Common;

using NUnit.Framework;

using System;

namespace LogTracker.Tests
{
    [TestFixture(TestOf = typeof(LogConfig))]
    public class LogConfigTest
    {
        [Test]
        public void IsValidDefault()
        {
            var config = new LogConfig();

            Assert.That(config.IsValid, Is.False);
        }

        [Test]
        public void IsValidOtherValues()
        {
            var config = new LogConfig();

            config.LogLinePath = "i'm not used";

            Assert.That(config.IsValid, Is.False);
        }
        
        [Test]
        public void IsValidValues([Values(null, "", "    ", "time")]string timestamp, [Values(null, "", "    ", "log")]string logPath)
        {
            var isValid = "time".Equals(timestamp) && "log".Equals(logPath);

            var config = new LogConfig();

            config.TimestampPath = timestamp;
            config.LogMessagePath = logPath;

            Assert.That(config.IsValid, Is.EqualTo(isValid));
        }

        [Test]
        public void VariableTest()
        {
            Assert.That(LogConfig.GetAttributePaths(), Has.Exactly(Enum.GetValues(typeof(LogAttribute)).Length - 1).Items); // Don't count LogSource
            Assert.That(Enum.GetValues(typeof(LogParserFailureHandling)), Has.Exactly(3).Items);
        }
    }
}