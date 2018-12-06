using Stethoscope.Log.Internal;
using Stethoscope.Printers.Internal;

using NUnit.Framework;

using System;

using Stethoscope.Log.Internal.Storage;

namespace Stethoscope.Tests
{
    public abstract class IOPrinterTests
    {
        protected LogRegistry logRegistry;
        protected LogConfig logConfig;

        [SetUp]
        public virtual void Setup()
        {
            logRegistry = new LogRegistry(new ListStorage());
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

        private System.Threading.Tasks.Task<string> PrintDataAsync(System.Threading.CancellationToken? cancellationToken = null)
        {
            // Yes, this could be rewritten as an async/await but I wanted to be explcit and not let the compiler do things it's way... but my way

            var printer = GetIOPrinter();
            printer.Setup();

            System.Threading.Tasks.Task printTask;
            if (cancellationToken.HasValue)
            {
                printTask = printer.PrintAsync(cancellationToken.Value);
            }
            else
            {
                printTask = printer.PrintAsync();
            }
            
            return printTask.ContinueWith(_ => printer.Teardown(), System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously)
                .ContinueWith(_ => GetPrintedData(), System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously);
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

        [Test]
        public void PopulatedLogEntryAsync()
        {
            AddLog("testentry", 123, "myFunc", "path/to/location.cpp");

            var expectedLogPrintout = "Thread 123\nStart myFunc // path/to/location.cpp\n  testentry\nEnd myFunc";

            var dataTask = PrintDataAsync();

            Assert.That(dataTask.Result, Is.EqualTo(expectedLogPrintout));
            // Would like to check task state, but state is reset in continuations... so it will return the state of the continuation instead
        }

        [Test]
        public void PopulatedLogEntryAsyncCancelled()
        {
            AddLog("testentry", 123, "myFunc", "path/to/location.cpp");
            
            var cancellationToken = new System.Threading.CancellationToken(true);
            var dataTask = PrintDataAsync(cancellationToken);

            Assert.That(dataTask.Result, Is.Empty);
            // Would like to check task state, but state is reset in continuations... so it will return the state of the continuation instead
        }
    }
}
