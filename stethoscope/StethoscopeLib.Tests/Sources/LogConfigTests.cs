using NUnit.Framework;

namespace LogTracker.Tests
{
    [TestFixture]
    public class LogConfigTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void Teardown()
        {
        }

        [Test]
        public void Test_IsValid_NoValues()
        {
            var config = new LogConfig();

            Assert.IsFalse(config.IsValid);
        }

        [Test]
        public void Test_IsValid_UnusedValueSet()
        {
            var config = new LogConfig();

            config.LogLinePath = "path";

            Assert.IsFalse(config.IsValid);
        }

        [Test]
        public void Test_IsValid_TimestampSet()
        {
            var config = new LogConfig();

            config.TimestampPath = "path";

            Assert.IsFalse(config.IsValid);
        }

        [Test]
        public void Test_IsValid_LogMessageSet()
        {
            var config = new LogConfig();

            config.LogMessagePath = "path";

            Assert.IsFalse(config.IsValid);
        }

        [Test]
        public void Test_IsValid_TimestampAndLogMessageSet()
        {
            var config = new LogConfig();

            config.TimestampPath = "path";
            config.LogMessagePath = "path";

            Assert.IsTrue(config.IsValid);
        }
    }
}