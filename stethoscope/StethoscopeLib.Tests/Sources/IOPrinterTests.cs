﻿using NSubstitute;

using NUnit.Framework;
using Stethoscope.Common;
using Stethoscope.Log.Internal;
using Stethoscope.Log.Internal.Storage;
using Stethoscope.Printers.Internal;

using System;
using System.Linq;
using System.Text;

namespace Stethoscope.Tests
{
    public abstract class IOPrinterTests
    {
        protected LogRegistry logRegistry;
        protected LogConfig logConfig;

        [SetUp]
        public virtual void Setup()
        {
            ElementFactory = null;
            logRegistry = new LogRegistry(new ListStorage());
            logConfig = new LogConfig()
            {
                TimestampPath = "TimePath",
                LogMessagePath = "MessagePath"
            };
        }

        [TearDown]
        public virtual void Teardown()
        {
            ElementFactory = null;
        }

        protected abstract string PrintedDataNewLine { get; }

        protected abstract void ResetPrintedData();
        protected abstract string GetPrintedData();
        protected abstract IOPrinter GetIOPrinter();

        protected IPrinterElementFactory ElementFactory { get; set; }

        private string PrintData()
        {
            // Cannot use "var" here as it returns IOPrinter, and that doesn't have the "default implementations" so it errors
            IPrinter printer = GetIOPrinter();
            printer.Setup();
            printer.Print();
            printer.Teardown();

            return GetPrintedData();
        }

        private System.Threading.Tasks.Task<string> PrintDataAsync(System.Threading.CancellationToken? cancellationToken = null)
        {
            // Yes, this could be rewritten as an async/await but I wanted to be explcit and not let the compiler do things it's way... but my way

            // Cannot use "var" here as it returns IOPrinter, and that doesn't have the "default implementations" so it errors
            IPrinter printer = GetIOPrinter();
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

        [Test, Explicit]
        public void DifferentThreads()
        {
            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc", "path/to/location.cpp");

            var expectedLogPrintout = "Thread 123\nStart myFunc // path/to/location.cpp\n  testentry1\nEnd myFunc\n\nThread 321\nStart myFunc // path/to/location.cpp\n  testentry2\nEnd myFunc";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test, Explicit]
        public void SameThreads()
        {
            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 123, "myFunc", "path/to/location.cpp");

            var expectedLogPrintout = "Thread 123\nStart myFunc // path/to/location.cpp\n  testentry1\n  testentry2\nEnd myFunc";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test, Explicit]
        public void DifferentFunctions()
        {
            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 123, "myOtherFunc", "path/to/location.cpp");

            var expectedLogPrintout = "Thread 123\nStart myFunc // path/to/location.cpp\n  testentry1\nEnd myFunc\nStart myOtherFunc // path/to/location.cpp\n  testentry2\nEnd myOtherFunc";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        // Same functions is the same as SameThreads

        [Test, Explicit]
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

        private void InternalPrintModeGeneralTest()
        {
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

        #region "Special Cases"

        [Test]
        public void PrintModeNullNext()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , null }
            };

            InternalPrintModeGeneralTest();
        }

        [Test]
        public void PrintModeEmptyText()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , string.Empty }
            };

            InternalPrintModeGeneralTest();
        }

        [Test]
        public void PrintModeWhitespaceText()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "    " }
            };

            InternalPrintModeGeneralTest();
        }

        [Test]
        public void PrintModeRandomText()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "hjfjhkfdjadf" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            Assert.Throws<ArgumentException>(() => PrintData());
        }

        #endregion

        #region General

        [Test]
        public void PrintModeGeneral()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"printMode" , "General" }
            };

            InternalPrintModeGeneralTest();
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

        [Test]
        public void PrintModeCustomEmpty()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            
            var expectedLogPrintout = string.Empty;

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeCustomRaw()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@hello world" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            var expectedLogPrintout = "hello world";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeCustomRawSpecialChar()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@@++--^^$$~~!!{{}}" } // @ isn't considered a special char, but because it is a marker, we'll test it
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            var expectedLogPrintout = "@+-^$~!{}";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeCustomRawSpecialCharInvalid()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@+-^$~!{}" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            Assert.Throws<ArgumentException>(() => PrintData());
        }

        [Test]
        public void PrintModeCustomAttribute()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@{Message}" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            var expectedLogPrintout = "testentry1";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        //TODO: @Hello{Message} -> Hello<message>
        //TODO: @{Message}Hello{{World -> <message>Hello{World
        //TODO: @Hello{{World{Message} -> Hello{World<message>
        //TODO: @Hello{{{Message} -> Hello{<message>
        //TODO: @{Message||} -> <exception>
        //TODO: @{Message|Hello{}World} -> Hello<message>World
        //TODO: @{Message|Hello}}World} -> Hello}}World
        //TODO: @{Message|Hello{{{}}}World} -> Hello{<message>}World
        //TODO: @{Message|Hello{{{}} -> Hello{<message>
        
        /* TODO:
         * 
         * test following char arrangements:
         * - "a}b"
         * - "a}}b"
         * - "a}}b}"
         * - "a}}}b"
         * 
         * Test areas:
         * - Before/between Parts
         * - Attribute reference (this will throw exceptions as no special char is in a valid attribute reference
         * - Attribute format
         */

        [Test]
        public void PrintModeCustomAttributeConditionExists()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@^{Message}^{Function}" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 321, null, "path/to/location.cpp");

            var expectedLogPrintout = "testentry1myFunc\ntestentry2";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeCustomAttributeConditionNotExists()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@^{Message}{Function}" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 321, null, "path/to/location.cpp");

            var expectedLogPrintout = "testentry1myFunc\ntestentry2{Missing Value for \"Function\"}";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeCustomAttributeConditionValueChange()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@${Function}" }
            };

            AddLog("testentry1", 123, "myFunc2", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc", "path/to/location.cpp");
            AddLog("testentry3", 456, "myFunc", "path/to/location.cpp");
            AddLog("testentry4", 654, "myFunc2", "path/to/location.cpp");

            var expectedLogPrintout = "myFunc2\nmyFunc\nmyFunc2";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeCustomAttributeConditionValueNew()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@~{Function}" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc2", "path/to/location.cpp");
            AddLog("testentry3", 312, "myFunc", "path/to/location.cpp");

            var expectedLogPrintout = "myFunc\nmyFunc2";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeCustomAttributeConditionValidOnly()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@+{Message}" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            var failedLog = logRegistry.AddFailedLog();
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Timestamp, DateTime.Now);
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Message, "testentry2");
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.ThreadID, 456);
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Function, "myFunc2");
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.SourceFile, "path/to/location.cpp");
            logRegistry.NotifyFailedLogParsed(failedLog);

            var expectedLogPrintout = "testentry1";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeCustomAttributeConditionInvalidOnly()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@-{Message}" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            var failedLog = logRegistry.AddFailedLog();
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Timestamp, DateTime.Now);
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Message, "testentry2");
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.ThreadID, 456);
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Function, "myFunc2");
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.SourceFile, "path/to/location.cpp");
            logRegistry.NotifyFailedLogParsed(failedLog);

            var expectedLogPrintout = "testentry2";

#if false
            var element = Substitute.For<Printers.Internal.PrintMode.IElement>();
            var modifier = Substitute.For<Printers.Internal.PrintMode.IModifier>();
            var conditional = Substitute.For<Printers.Internal.PrintMode.IConditional>();
            ElementFactory = Substitute.For<IPrinterElementFactory>();

            ElementFactory.CreateRaw(null).ReturnsForAnyArgs(element);
            ElementFactory.CreateElement(Arg.Any<Common.LogAttribute>(), null, null, null).ReturnsForAnyArgs(element);
            ElementFactory.CreateModifier(Arg.Any<ModifierElement>()).ReturnsForAnyArgs(modifier);
            ElementFactory.CreateConditional(Arg.Any<ConditionalElement>()).ReturnsForAnyArgs(conditional);

            conditional.ShouldProcess(null, null).ReturnsForAnyArgs(false);
#endif

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        //XXX - valid log , invalid log
        //TODO: @v{Message} -> <+message>
        //TODO: @i{Message} -> <-message>
        //TODO: @vi{Message} -> <error from parser>
        //TODO: @v+{Message} -> <+message>
        //TODO: @v-{Message} -> <error from parser>
        //TODO: @i+{Message} -> <error from parser>
        //TODO: @i-{Message} -> <-message>
        //TODO: @v+{Message}-{Message} -> <+message>
        //TODO: @i+{Message}-{Message} -> <-message>
        //TODO: @vv{Message} -> v<+message> , v<-message>
        //TODO: @ii{Message} -> i<+message> , i<-message>
        //TODO: @vvv{Message} -> v<+message>
        //TODO: @iii{Message} -> i<-message>
        //TODO: @vii{Message} -> i<+message>
        //TODO: @ivv{Message} -> v<-message>

        // Only test a couple combos as all combos are a non-repetitious permutation (5 fields, in different orders, could result in as many as 5! = 120 combos. Not writing 120 tests...)

        [TestCase("@^${SourceFile}", ExpectedResult = "path/to/location.cpp\npath/to/location2.cpp\npath/to/location.cpp\npath/to/location2.cpp\npath/to/location.cpp")]
        [TestCase("@$^{SourceFile}", ExpectedResult = "path/to/location.cpp\npath/to/location2.cpp\npath/to/location.cpp\n{Missing Value for \"SourceFile\"}\npath/to/location2.cpp\npath/to/location.cpp\n{Missing Value for \"SourceFile\"}")]
        [TestCase("@^~{SourceFile}", ExpectedResult = "path/to/location.cpp\npath/to/location2.cpp")]
        [TestCase("@~^{SourceFile}", ExpectedResult = "path/to/location.cpp\npath/to/location2.cpp\n{Missing Value for \"SourceFile\"}\n{Missing Value for \"SourceFile\"}")]
        [TestCase("@^+{ThreadID}", ExpectedResult = "123\n345")]
        [TestCase("@^-{ThreadID}", ExpectedResult = "456\n112\n112")]
        [TestCase("@$+{Function}", ExpectedResult = "myFunc\nmyFunc2\nmyFunc")]
        [TestCase("@$-{Function}", ExpectedResult = "myFunc2\nmyFunc\nmyFunc2")]
        [TestCase("@~+{Message}", ExpectedResult = "testentry1")]
        [TestCase("@~-{Message}", ExpectedResult = "testentry2")]
        public string PrintModeCustomAttributeConditionCombo(string format)
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , format }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry1", 345, "myFunc2", "path/to/location2.cpp");
            AddLog("testentry1", null, "myFunc", "path/to/location.cpp");
            AddLog("testentry1", null, "myFunc", null);

            var failedLog = logRegistry.AddFailedLog();
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Timestamp, DateTime.Now);
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Message, "testentry2");
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.ThreadID, 456);
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Function, "myFunc2");
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.SourceFile, "path/to/location2.cpp");
            logRegistry.NotifyFailedLogParsed(failedLog);

            failedLog = logRegistry.AddFailedLog();
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Timestamp, DateTime.Now);
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Message, "testentry2");
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.ThreadID, null);
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Function, "myFunc");
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.SourceFile, "path/to/location.cpp");
            logRegistry.NotifyFailedLogParsed(failedLog);

            failedLog = logRegistry.AddFailedLog();
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Timestamp, DateTime.Now);
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Message, "testentry2");
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.ThreadID, 112);
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Function, "myFunc2");
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.SourceFile, "path/to/location.cpp");
            logRegistry.NotifyFailedLogParsed(failedLog);

            failedLog = logRegistry.AddFailedLog();
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Timestamp, DateTime.Now);
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Message, "testentry2");
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.ThreadID, 112);
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.Function, "myFunc2");
            logRegistry.AddValueToLog(failedLog, Common.LogAttribute.SourceFile, null);
            logRegistry.NotifyFailedLogParsed(failedLog);

            return PrintData();
        }
        
        [Test]
        public void PrintModeCustomAttributeConditionComboInvalid()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@+-{Message}" }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            Assert.Throws<ArgumentException>(() => PrintData());
        }

        [Test]
        public void PrintModeCustomAttributeModifierErrorHandler()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@{Function}!\"Oh hai\"" }
            };

            AddLog("testentry1", 123, null, "path/to/location.cpp");

            var expectedLogPrintout = "Oh hai";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeCustomAttributeModifierErrorHandlerInnerQuotes()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@{Function}!\"Oh hai \"steve\"\"" }
            };

            AddLog("testentry1", 123, null, "path/to/location.cpp");

            var expectedLogPrintout = "Oh hai \"steve\"";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [Test]
        public void PrintModeCustomAttributeConditionAndModifier()
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , "@~{Function}!\"Ahhh\"" }
            };

            AddLog("testentry1", 123, "myFunc2", "path/to/location.cpp");
            AddLog("testentry2", 321, null, "path/to/location.cpp");
            AddLog("testentry3", 456, null, "path/to/location.cpp");
            AddLog("testentry4", 654, "myFunc2", "path/to/location.cpp");

            var expectedLogPrintout = "myFunc2\nAhhh\nAhhh";

            var data = PrintData();

            Assert.That(data, Is.EqualTo(expectedLogPrintout));
        }

        [TestCase("@{Message} hello", ExpectedResult = "testentry1 hello")]
        [TestCase("@hello {Message}", ExpectedResult = "hello testentry1")]
        [TestCase("@hello {Message} stranger", ExpectedResult = "hello testentry1 stranger")]
        public string PrintModeCustomAttributeAndRaw(string format)
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , format }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");
            
            return PrintData();
        }

        [TestCase("@{Message|}", ExpectedResult = "")]
        [TestCase("@{Message|hello world}", ExpectedResult = "hello world")]
        [TestCase("@{Message|hello {}}", ExpectedResult = "hello testentry1")]
        [TestCase("@{Message|hello {} and {}}", ExpectedResult = "hello testentry1 and testentry1")]
        public string PrintModeCustomAttributeFormat(string format)
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , format }
            };

            AddLog("testentry1", 123, "myFunc", "path/to/location.cpp");

            return PrintData();
        }

        private void CompareFormats(string format1, string format2)
        {
            logConfig.ExtraConfigs = new System.Collections.Generic.Dictionary<string, string>()
            {
                { "printMode" , format1 }
            };

            AddLog("testentry1", 123, "myFunc2", "path/to/location.cpp");
            AddLog("testentry2", 321, "myFunc", "path/to/location.cpp");
            AddLog("testentry3", 456, "myFunc", "path/to/location.cpp");
            AddLog("testentry4", 654, "myFunc2", "path/to/location.cpp");
            AddLog("testentry6", 123, "myFunc", "path/to/location.cpp");
            AddLog("testentry7", 321, "myFunc2", "path/to/location.cpp");
            AddLog("testentry8", 312, "myFunc", "path/to/location.cpp");

            var str1 = PrintData();

            ResetPrintedData();

            logConfig.ExtraConfigs["printMode"] = format2;

            var str2 = PrintData();

            Assert.That(str1, Is.EqualTo(str2));
        }

        [Test]
        public void PrintModeCompareGeneral()
        {
            var sb = new StringBuilder("@!\"Problem printing log. Timestamp=^{Timestamp}, Message=^{Message}\"[{Timestamp}] -- {Message}");
            foreach (var e in Enum.GetValues(typeof(Common.LogAttribute)).Cast<Common.LogAttribute>())
            {
                if (e == Common.LogAttribute.Timestamp || e == Common.LogAttribute.Message)
                {
                    continue;
                }
                sb.AppendFormat("^{{{0}|, {0}=\"{{}}\"}}", e);
            }

            CompareFormats("General", sb.ToString());
        }

        [Test]
        public void PrintModeCompareFunctionOnly()
        {
            CompareFormats("FunctionOnly", "@{Function}!\"@vLog is missing Function attribute: {Timestamp} -- {Message}\"");
        }

        [Test]
        public void PrintModeCompareFirstFunctionOnly()
        {
            CompareFormats("FirstFunctionOnly", "@~{Function}!\"@vLog is missing Function attribute: {Timestamp} -- {Message}\"");
        }

        [Test]
        public void PrintModeCompareDifferentFunctionOnly()
        {
            CompareFormats("DifferentFunctionOnly", "@${Function}!\"@vLog is missing Function attribute: {Timestamp} -- {Message}\"");
        }
        
#endregion

#endregion
    }
}
