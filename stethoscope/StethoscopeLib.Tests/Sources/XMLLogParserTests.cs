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

        //TODO: SetConfig

        //TODO: Parse
    }
}
