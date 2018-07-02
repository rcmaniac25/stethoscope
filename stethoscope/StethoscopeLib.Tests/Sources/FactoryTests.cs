using NSubstitute;

using NUnit.Framework;

using Stethoscope.Common;
using Stethoscope.Log;
using Stethoscope.Parsers;
using Stethoscope.Printers;

namespace Stethoscope.Tests
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

        [Test(TestOf = typeof(LogRegistryFactory))]
        public void LogRegistryFactoryProducesRegistry()
        {
            var factory = LogRegistryFactory.Create();

            Assert.That(factory, Is.Not.Null);

            var registry = factory.Create();

            Assert.That(registry, Is.Not.Null);
            Assert.That(registry, Is.TypeOf<Log.Internal.LogRegistry>());
        }

        private static Log.Internal.IRegistryStorage LogRegistryFactoryStorageTest(RegistrySelectionCriteria criteria)
        {
            var factory = LogRegistryFactory.Create();

            Assert.That(factory, Is.Not.Null);

            var factoryType = typeof(LogRegistryFactory).GetNestedType("LogRegistryFactoryFinder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var selectionCriteria = factoryType.GetMethod("PickStorage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return selectionCriteria.Invoke(factory, new object[] { criteria }) as Log.Internal.IRegistryStorage;
        }

        [Test(TestOf = typeof(LogRegistryFactory))]
        public void LogRegistryFactoryStorageNull()
        {
            var storage = LogRegistryFactoryStorageTest(RegistrySelectionCriteria.Null);
            Assert.That(storage, Is.Not.Null);
            Assert.That(storage, Is.TypeOf<Log.Internal.Storage.NullStorage>());
            Assert.That(storage.SortAttribute, Is.EqualTo(LogAttribute.Timestamp));
        }

        [Test(TestOf = typeof(LogRegistryFactory))]
        public void LogRegistryFactoryStorageDefault()
        {
            var storage = LogRegistryFactoryStorageTest(RegistrySelectionCriteria.Default);
            Assert.That(storage, Is.Not.Null);
            Assert.That(storage, Is.TypeOf<Log.Internal.Storage.ListStorage>());
            Assert.That(storage.SortAttribute, Is.EqualTo(LogAttribute.Timestamp));
        }
    }
}
