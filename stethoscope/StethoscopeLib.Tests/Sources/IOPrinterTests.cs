using LogTracker.Log;
using LogTracker.Printers.Internal;

using NUnit.Framework;

using System;

namespace LogTracker.Tests.Sources
{
    public abstract class IOPrinterTests
    {
        protected LogRegistry logRegistry;
        protected LogConfig logConfig;

        [SetUp]
        public virtual void Setup()
        {
            logRegistry = new LogRegistry();
            logConfig = new LogConfig()
            {
                TimestampPath = "TimePath",
                LogMessagePath = "MessagePath"
            };
        }

        protected abstract string PrintedDataNewLine { get; }

        protected abstract string GetPrintedData();
        protected abstract IOPrinter GetIOPrinter();

        private string PrintData()
        {
            var printer = GetIOPrinter();
            printer.Setup();
            printer.Print();
            printer.Teardown();

            return GetPrintedData();
        }

        [Test]
        public void SetupAndTeardown()
        {
            var printer = GetIOPrinter();
            printer.Setup();
            printer.Teardown();

            Assert.That(GetPrintedData(), Is.Empty);
        }

        [Test]
        public void NoLogs()
        {
            var data = PrintData();

            Assert.That(data, Is.Empty);
        }

        [Test]
        public void SimpleLogEntry()
        {
            var entry = logRegistry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            var data = PrintData();

            //XXX is this what we're expecting?
            Assert.That(data, Is.Empty);
        }

        //TODO: tests
    }
}
