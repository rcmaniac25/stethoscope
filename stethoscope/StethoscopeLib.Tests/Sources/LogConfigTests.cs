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

        // Pairwise/combinatorial would be useful, but I need to check the expected result value
        // XXX add null values
        [TestCase("", "", ExpectedResult = false)]
        [TestCase("    ", "", ExpectedResult = false)]
        [TestCase("", "    ", ExpectedResult = false)]
        [TestCase("    ", "    ", ExpectedResult = false)]
        [TestCase("time", "", ExpectedResult = false)]
        [TestCase("", "log", ExpectedResult = false)]
        [TestCase("time", "    ", ExpectedResult = false)]
        [TestCase("    ", "log", ExpectedResult = false)]
        [TestCase("time", "log", ExpectedResult = true)]
        public bool IsValidValuesTest(string timestamp, string logMessage)
        {
            var config = new LogConfig();

            config.TimestampPath = timestamp;
            config.LogMessagePath = logMessage;

            return config.IsValid;
        }

        [Test]
        public void GetAttributePaths()
        {
            Assert.That(LogConfig.GetAttributePaths(), Has.Exactly(Enum.GetValues(typeof(LogAttribute)).Length).Items);
        }
    }
}