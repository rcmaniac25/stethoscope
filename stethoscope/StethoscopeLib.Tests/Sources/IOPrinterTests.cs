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

        #region PrintMode

        #region "Special Cases"

        //TODO: printMode null

        //TODO: printMode empty string

        //TODO: printMode random text

        #endregion

        #region General

        [Test]
        public void PrintModeGeneral()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "General" }
            };

            var now = DateTime.Now;
            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            //@!"Problem printing log. Timestamp=^{Timestamp}, Message=^{Message}"[{Timestamp}] -- {Message}^{LogSource|, LogSource="{}"}^{ThreadID|, ThreadID="{}"}...^{Context|, Context="{}"}
            var expectedLogPrintout = " - testentry1, ThreadID=\"123\", SourceFile=\"path/to/location.cpp\", Function=\"myFunc\"";

            var data = PrintData();

            Assert.That(data.StartsWith('['), Is.True);
            Assert.That(data.IndexOf(']'), Is.GreaterThan(0));

            var dtString = data.Substring(1, data.IndexOf(']') - 1);
            Assert.That(DateTime.TryParse(dtString, out DateTime date), Is.True);
            Assert.That(date, Is.EqualTo(now).Within(TimeSpan.FromSeconds(1)));

            var logPrint = data.Substring(data.IndexOf(']') + 1);

            Assert.That(logPrint, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeGeneralFailure()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "General" }
            };

            var now = DateTime.Now;
            var failedLog = logRegistry.AddFailedLog();
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Timestamp, now);
            logRegistry.NotifyFailedLogParsed(failedLog);

            //@!"Problem printing log. Timestamp=^{Timestamp}, Message=^{Message}"[{Timestamp}] -- {Message}^{LogSource|, LogSource="{}"}^{ThreadID|, ThreadID="{}"}...^{Context|, Context="{}"}
            var expectedLogPrintout = $"Problem printing log. Timestamp={now}, Message=";

            var data = PrintData();
            
            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        #endregion

        #region FunctionOnly

        [Test]
        public void PrintModeFunctionOnly()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "FunctionOnly" }
            };
            
            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            //@{Function}!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = "myFunc";

            var data = PrintData();
            
            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeFunctionOnlyMulti()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "FunctionOnly" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc2", "path/to/location.cpp");

            //@{Function}!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = "myFunc\nmyFunc2";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeFunctionOnlyFailure()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "FunctionOnly" }
            };

            var nowString = DateTime.Now.ToString();
            logRegistry.AddLog(nowString, "testentry1");

            //@{Function}!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = $"Log is missing Function attribute: {nowString} - testentry1";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeFunctionOnlyFailureInvalidLog()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "FunctionOnly" }
            };

            var now = DateTime.Now;
            var failedLog = logRegistry.AddFailedLog();
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Timestamp, DateTime.Now);
            logRegistry.NotifyFailedLogParsed(failedLog);

            //@{Function}!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = string.Empty;

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        #endregion

        #region FirstFunctionOnly

        [Test]
        public void PrintModeFirstFunctionOnly()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "FirstFunctionOnly" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            //@{Function}~!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = "myFunc";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeFirstFunctionOnlyMultiDiff()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "FirstFunctionOnly" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc2", "path/to/location.cpp");

            //@{Function}~!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = "myFunc\nmyFunc2";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeFirstFunctionOnlyMultiSame()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "FirstFunctionOnly" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc", "path/to/location.cpp");

            //@{Function}~!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = "myFunc";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeFirstFunctionOnlyMulti()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "FirstFunctionOnly" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc2", "path/to/location.cpp");
            AddLog("testentry3", 312, "myFunc", "path/to/location.cpp");

            //@{Function}~!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = "myFunc\nmyFunc2";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        //FirstFunctionOnly uses the same format as FunctionOnly, but just when it prints results

        #endregion

        #region DifferentFunctionOnly

        [Test]
        public void PrintModeDifferentFunctionOnly()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "DifferentFunctionOnly" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            //@{Function}$!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = "myFunc";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeDifferentFunctionOnlyDiff()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "DifferentFunctionOnly" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc2", "path/to/location.cpp");

            //@{Function}$!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = "myFunc\nmyFunc2";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeDifferentFunctionOnlySame()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "DifferentFunctionOnly" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc", "path/to/location.cpp");

            //@{Function}$!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = "myFunc";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeDifferentFunctionOnlySequence1()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "DifferentFunctionOnly" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc2", "path/to/location.cpp");
            AddLog("testentry3", 456, "myFunc", "path/to/location.cpp");
            AddLog("testentry4", 654, "myFunc2", "path/to/location.cpp");

            //@{Function}$!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = "myFunc\nmyFunc2\nmyFunc\nmyFunc2";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeDifferentFunctionOnlySequence2()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "DifferentFunctionOnly" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc", "path/to/location.cpp");
            AddLog("testentry3", 456, "myFunc2", "path/to/location.cpp");
            AddLog("testentry4", 654, "myFunc2", "path/to/location.cpp");

            //@{Function}$!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = "myFunc\nmyFunc2";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeDifferentFunctionOnlySequence3()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "DifferentFunctionOnly" }
            };
            
            AddLog("testentry1", 123, "myFunc2", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc", "path/to/location.cpp");
            AddLog("testentry3", 456, "myFunc", "path/to/location.cpp");
            AddLog("testentry4", 654, "myFunc2", "path/to/location.cpp");

            //@{Function}$!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = "myFunc2\nmyFunc\nmyFunc2";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        //DifferentFunctionOnly uses the same format as FunctionOnly, but just when it prints results

        #endregion

        #region Custom

#if false
        [Test]
        public void PrintModeNull()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , null }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            //@{Function}!"@+Log is missing Function attribute: {Timestamp} -- {Message}"
            var expectedLogPrintout = "myFunc";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }
#endif

        //TODO: printMode @

        //TODO: printMode @text

        //TODO: printMode @<special char>

        //TODO: ...the many possibilities with formats

        #endregion

        #endregion
    }
}
