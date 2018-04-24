using System;
using System.IO;

using LogTracker.Log;
using LogTracker.Printers.Internal;

using NUnit.Framework;

namespace LogTracker.Tests.Sources
{
    // Since we're replacing standard out, we can't run this is parallel
    [TestFixture(TestOf = typeof(ConsolePrinter)), NonParallelizable]
    public class ConsolePrinterTests
    {
        private TextWriter _originalStdOut;
        private StringWriter _fakeStdOut;

        private LogRegistry logRegistry;
        private LogConfig logConfig;

        [SetUp]
        public void Setup()
        {
            _originalStdOut = Console.Out;

            _fakeStdOut = new StringWriter();
            Console.SetOut(_fakeStdOut);

            logRegistry = new LogRegistry();
            logConfig = new LogConfig()
            {
                TimestampPath = "TimePath",
                LogMessagePath = "MessagePath"
            };
        }

        [TearDown]
        public void Teardown()
        {
            Console.SetOut(_originalStdOut);
        }

        private string GetConsoleOutput()
        {
            return _fakeStdOut.ToString();
        }

        private ConsolePrinter CreateConsolePrinter()
        {
            //XXX Should we just use a factory?

            var printer = new ConsolePrinter();
            printer.SetConfig(logConfig);
            printer.SetRegistry(logRegistry);
            return printer;
        }

        [Test]
        public void ConsoleSanityTest()
        {
            Console.WriteLine("test msg");
            Assert.That(GetConsoleOutput(), Is.EqualTo($"test msg{_originalStdOut.NewLine}")); //XXX how can this be made... less ugly
        }

        [Test]
        public void NoLogs()
        {
            var printer = CreateConsolePrinter();
            printer.Print();
        }

        //TODO: tests
    }
}
