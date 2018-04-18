using LogTracker.Common;
using LogTracker.Parsers;
using LogTracker.Printers;

using NSubstitute;

using NUnit.Framework;

namespace LogTracker.Tests
{
    [TestFixture]
    public class FactoryTests
    {
        private ILogRegistry mockLogRegistry;
        private LogConfig logConfig;

        [SetUp]
        public void Setup()
        {
            mockLogRegistry = Substitute.For<ILogRegistry>();
            logConfig = new LogConfig()
            {
                TimestampPath = "TimePath",
                LogMessagePath = "MessagePath"
            };
        }

        [TearDown]
        public void Teardown()
        {
        }

        [Test(TestOf = typeof(PrinterFactory))]
        public void PrinterFactoryProducesConsole()
        {
            var factory = PrinterFactory.CrateConsoleFactory();

            Assert.That(factory, Is.Not.Null);

            var printer = factory.Create(mockLogRegistry, logConfig);

            Assert.That(printer, Is.Not.Null);
            Assert.That(printer, Is.TypeOf<Printers.Internal.ConsolePrinter>());
        }

        [Test(TestOf = typeof(LogParserFactory))]
        public void LogParserFactoryUnknownFileExtension()
        {
            var factory = LogParserFactory.GetParserForFileExtension("notAnExt");

            Assert.That(factory, Is.Null);
        }

        [Test(TestOf = typeof(LogParserFactory))]
        public void LogParserFactoryXmlParser()
        {
            var factory = LogParserFactory.GetParserForFileExtension("xml");

            Assert.That(factory, Is.Not.Null);

            var parser = factory.Create(mockLogRegistry, logConfig);

            Assert.That(parser, Is.Not.Null);
            Assert.That(parser, Is.TypeOf<Parsers.Internal.XML.XMLLogParser>());
        }
    }
}
