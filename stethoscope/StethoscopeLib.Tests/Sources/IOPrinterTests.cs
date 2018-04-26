﻿using LogTracker.Log;
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

        /*
         * For now (4/26/2018) this is the general format of printing
         * 
         * Thread num
         * Start funct // ../my/src/path.cpp
         *   log message
         * End funct
         * 
         * Thread num
         * Start funct // ../my/src/path.cpp
         *   log message
         *   log message
         * End funct
         */

        [Test]
        public void SimpleLogEntry()
        {
            var entry = logRegistry.AddLog(DateTime.Now.ToString(), "testmsg");
            Assert.That(entry, Is.Not.Null);

            var data = PrintData();
            
            Assert.That(data, Is.Empty);
        }

        private void AddLog(string message, object thread, object function, object path, int timeOffsetSec = -1, bool assertCreated = false)
        {
            var time = DateTime.Now;
            if (timeOffsetSec >= 0)
            {
                time = new DateTime(2018, 4, 26, 0, 38, 58).AddSeconds(timeOffsetSec);
            }

            var entry = logRegistry.AddLog(time.ToString(), message);
            if (assertCreated)
            {
                Assert.That(entry, Is.Not.Null);
            }

            logRegistry.AddValueToLog(entry, Common.LogAttribute.ThreadID, thread);
            logRegistry.AddValueToLog(entry, Common.LogAttribute.Function, function);
            logRegistry.AddValueToLog(entry, Common.LogAttribute.SourceFile, path);
        }

        [Test]
        public void PopulatedLogEntry()
        {
            AddLog("testentry", 123, "myFunc", "path/to/location.cpp");

            var expectedLogPrintout = "Thread 123\nStart myFunc // path/to/location.cpp\n  testentry\nEnd myFunc";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void DifferentThreads()
        {
            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc", "path/to/location.cpp");

            var expectedLogPrintout = "Thread 123\nStart myFunc // path/to/location.cpp\n  testentry1\nEnd myFunc\n\nThread 321\nStart myFunc // path/to/location.cpp\n  testentry2\nEnd myFunc";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void SameThreads()
        {
            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 123, "myFunc", "path/to/location.cpp");

            var expectedLogPrintout = "Thread 123\nStart myFunc // path/to/location.cpp\n  testentry1\n  testentry2\nEnd myFunc";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void DifferentFunctions()
        {
            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 123, "myOtherFunc", "path/to/location.cpp");

            var expectedLogPrintout = "Thread 123\nStart myFunc // path/to/location.cpp\n  testentry1\nEnd myFunc\nStart myOtherFunc // path/to/location.cpp\n  testentry2\nEnd myOtherFunc";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        // Same functions is the same as SameThreads

        [Test]
        public void SameSources()
        {
            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 123, "myFunc", "path/to/other/location.cpp");

            var expectedLogPrintout = "Thread 123\nStart myFunc // path/to/location.cpp\n  testentry1\nEnd myFunc\nStart myFunc // path/to/other/location.cpp\n  testentry2\nEnd myFunc";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        // Same sources is the same as SameThreads
    }
}
