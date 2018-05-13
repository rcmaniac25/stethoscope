using LogTracker.Common;
using LogTracker.Parsers.Internal.XML;

using NSubstitute;

using NUnit.Framework;

using System.IO;
using System.Text;

namespace LogTracker.Tests
{
    [TestFixture(TestOf = typeof(XMLLogParser))]
    public class XMLLogParserTests
    {
        private ILogRegistry logRegistry;
        private ILogEntry logEntry;
        private LogConfig logConfig;

        [SetUp]
        public void Setup()
        {
            logRegistry = Substitute.For<ILogRegistry>();
            logEntry = Substitute.For<ILogEntry>();
            logConfig = new LogConfig()
            {
                TimestampPath = "!time",
                LogMessagePath = "!log"
            };
        }

        private static MemoryStream CreateStream(string data)
        {
            var ms = new MemoryStream();

            var dataBytes = Encoding.UTF8.GetBytes(data);
            ms.Write(dataBytes, 0, dataBytes.Length);

            ms.Position = 0L;

            return ms;
        }

        [Test]
        public void BasicTest()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received().AddLog("goodtime", "my log");
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void MultipleBasicLogs()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", Arg.Any<string>()).Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log1\"></fakelog><fakelog time=\"goodtime\" log=\"my log2\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received().AddLog("goodtime", Arg.Is<string>(x => x.StartsWith("my log")));
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void NoConfig()
        {
            var parser = new XMLLogParser();
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", Arg.Any<string>()).Returns(logEntry);
            
            var log = "<fakelog time=\"goodtime\" log=\"my log1\"></fakelog><fakelog time=\"goodtime\" log=\"my log2\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), Arg.Any<string>());
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void NoRegistry()
        {
            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);

            var log = "<fakelog time=\"goodtime\" log=\"my log1\"></fakelog><fakelog time=\"goodtime\" log=\"my log2\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }
        }

        [Test]
        public void SetConfigInvalidTimestampPath()
        {
            logConfig.TimestampPath = "!";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), Arg.Any<string>());
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void SetConfigInvalidLogMessagePath()
        {
            logConfig.LogMessagePath = "!";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);

            var log = "<fakelog time=\"goodtime\" log=\"my log\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.DidNotReceive().AddLog(Arg.Any<string>(), Arg.Any<string>());
            logRegistry.DidNotReceive().AddValueToLog(logEntry, Arg.Any<LogAttribute>(), Arg.Any<object>());
        }

        [Test]
        public void SetConfigAdditionalAttribute()
        {
            //TODO: would be good to test every attribute

            logConfig.LogTypePath = "!type";

            var parser = new XMLLogParser();
            parser.SetConfig(logConfig);
            parser.SetRegistry(logRegistry);

            logRegistry.AddLog("goodtime", "my log").Returns(logEntry);
            logRegistry.AddValueToLog(logEntry, LogAttribute.Type, "colorful").Returns(true);

            var log = "<fakelog time=\"goodtime\" log=\"my log\" type=\"colorful\"></fakelog>";
            using (var ms = CreateStream(log))
            {
                parser.Parse(ms);
            }

            logRegistry.Received().AddLog("goodtime", "my log");
            logRegistry.Received().AddValueToLog(logEntry, LogAttribute.Type, "colorful");
        }

        //TODO: Parse
        // - (including testing "a" bad log. Maybe skip it or mark it's invalid? Config option?)
    }
}
