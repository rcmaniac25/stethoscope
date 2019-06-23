using Metrics;

using Stethoscope.Common;
using Stethoscope.Printers.Internal.PrintMode;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stethoscope.Printers.Internal
{
    /// <summary>
    /// Log printer that prints to an I/O object of type <see cref="TextReader"/>.
    /// </summary>
    public abstract class IOPrinter : BaseIPrinter
    {
        private const string DefaultPrinterName = "General";

        /// <summary>
        /// Metric counter for indicating every time <see cref="PrintAsync"/> is invoked.
        /// </summary>
        protected static readonly Counter printCounter = Metric.Counter("IO Printer Print", Unit.Calls, "IO, printer");

        private static readonly LogAttribute[] knownAttributes = Enum.GetValues(typeof(LogAttribute)).Cast<LogAttribute>().Where(att => att != LogAttribute.Timestamp && att != LogAttribute.Message).ToArray();
        private static readonly IDictionary<string, (Action<TextWriter, ILogEntry, object> printer, Func<object> printerStateGen)> PrintHandlers = new Dictionary<string, (Action<TextWriter, ILogEntry, object>, Func<object>)>();

        static IOPrinter()
        {
            InitializePrintHandlers();
        }

        /// <summary>
        /// The log registry that logs will be retrieved from.
        /// </summary>
        protected ILogRegistry logRegistry;

        /// <summary>
        /// The TextWriter which will be printed to.
        /// </summary>
        protected TextWriter TextWriter { get; set; }

        /// <summary>
        /// Cancellable print function invoked by <see cref="IPrinter.Print"/> or <see cref="IPrinter.PrintAsync()"/> or <see cref="IPrinter.PrintAsync(CancellationToken)"/>.
        /// </summary>
        protected Action<CancellationToken> LogPrintHandler { get; set; }

        /// <summary>
        /// Produce indentation for printing strings.
        /// </summary>
        /// <param name="indent">The indent level, each representing 2 spaces.</param>
        /// <returns>A string of the specified indentation.</returns>
        protected static string GenerateIndentLog(int indent) => new string(' ', indent * 2); //XXX config for indent size

        /// <summary>
        /// Print all logs, by thread (<see cref="LogAttribute.ThreadID"/>), to <see cref="TextReader"/>.
        /// </summary>
        protected void PrintThreadTraces()
        {
            //XXX How would this do with giant, never-ending streams of data?
            var lastThread = 
                logRegistry.GetBy(LogAttribute.ThreadID).SelectMany(group =>
                {
                    return Observable.Return(group.Key).Concat(group);
                }).Aggregate(Tuple.Create((object)null, false, "", ""), (thread, value) => // The one downside of using a tuple, lack of names: <thread ID>, isNewThread, lastFunc, lastFuncSrc
                {
                    if (value is ILogEntry log)
                    {
                        var ret = thread;
                        if (thread.Item2)
                        {
                            var initSrc = log.GetAttribute<string>(LogAttribute.SourceFile);
                            var lastFunc = log.GetAttribute<string>(LogAttribute.Function);

                            var lastFuncSrc = $"{initSrc}.{lastFunc}";
                            ret = Tuple.Create(thread.Item1, thread.Item2, lastFunc, lastFuncSrc);

                            TextWriter.WriteLine($"Start {lastFunc} // {initSrc}");
                        }

                        var src = log.GetAttribute<string>(LogAttribute.SourceFile);
                        var function = log.GetAttribute<string>(LogAttribute.Function);
                        var funcSrc = $"{src}.{function}";

                        if (!ret.Item2 && funcSrc != thread.Item4)
                        {
                            TextWriter.WriteLine($"End {thread.Item3}");
                            TextWriter.WriteLine($"Start {function} // {src}");
                            
                            ret = Tuple.Create(ret.Item1, ret.Item2, function, funcSrc);
                        }

                        TextWriter.WriteLine($"{GenerateIndentLog(1)}{log.Message}");

                        return Tuple.Create(ret.Item1, false, ret.Item3, ret.Item4);
                    }
                    else
                    {
                        if (thread.Item1 != null)
                        {
                            TextWriter.WriteLine($"End {thread.Item3}");
                            TextWriter.WriteLine();
                        }
                        TextWriter.WriteLine($"Thread {value}");
                        return Tuple.Create(value, true, "", "");
                    }
                }).LastOrDefaultAsync().Wait();
            if (lastThread.Item1 != null)
            {
                TextWriter.WriteLine($"End {lastThread.Item3}");
                TextWriter.WriteLine();
            }
        }

        #region PrintMode Handlers

        private static void InitializePrintHandlers()
        {
            PrintHandlers.Clear();

            PrintHandlers.Add(DefaultPrinterName.ToLower(), (PrintModeDefault, PrintModeStateGenNone));
            PrintHandlers.Add("FunctionOnly".ToLower(), (PrintModeFunctionOnly, PrintModeStateGenNone));
            PrintHandlers.Add("FirstFunctionOnly".ToLower(), (PrintModeFirstFunctionOnly, PrintModeStateGenFirstFunctionOnly));
            PrintHandlers.Add("DifferentFunctionOnly".ToLower(), (PrintModeDifferentFunctionOnly, PrintModeStateGenDifferentFunctionOnly));
        }

        private static object PrintModeStateGenNone() => null;

        private static void PrintModeDefault(TextWriter textWriter, ILogEntry log, object state)
        {
            // Equiv: @!"Problem printing log. Timestamp=^{Timestamp}, Message=^{Message}"[{Timestamp}] -- {Message}^{LogSource|, LogSource="{}"}^{ThreadID|, ThreadID="{}"}...^{Context|, Context="{}"}
            // printing every attribute

            try
            {
                var sb = new StringBuilder();
                sb.AppendFormat("[{0}] - {1}", log.Timestamp, log.Message);
                foreach (var att in knownAttributes)
                {
                    if (log.HasAttribute(att))
                    {
                        sb.AppendFormat(", {0}=\"{1}\"", att, log.GetAttribute<object>(att));
                    }
                }
                textWriter.WriteLine(sb.ToString());
            }
            catch
            {
                object timestamp;
                string message = string.Empty;
                if (log.IsValid)
                {
                    timestamp = log.Timestamp;
                    message = log.Message;
                }
                else
                {
                    if (log.HasAttribute(LogAttribute.Timestamp))
                    {
                        timestamp = log.Timestamp;
                    }
                    else
                    {
                        timestamp = string.Empty;
                    }
                    if (log.HasAttribute(LogAttribute.Message))
                    {
                        message = log.Message;
                    }
                }

                textWriter.WriteLine("Problem printing log. Timestamp={0}, Message={1}", timestamp, message);
            }
        }

        private static void PrintModeFunctionOnly(TextWriter textWriter, ILogEntry log, object state)
        {
            // Equiv: @{Function}!"@vLog is missing Function attribute: {Timestamp} -- {Message}"
            
            if (log.HasAttribute(LogAttribute.Function))
            {
                textWriter.WriteLine(log.GetAttribute<object>(LogAttribute.Function));
            }
            else if (log.IsValid)
            {
                textWriter.WriteLine("Log is missing Function attribute: {0} - {1}", log.Timestamp, log.Message);
            }
        }

        private static object PrintModeStateGenFirstFunctionOnly() => new HashSet<object>();

        private static void PrintModeFirstFunctionOnly(TextWriter textWriter, ILogEntry log, object state)
        {
            // Equiv: @{Function}~!"@vLog is missing Function attribute: {Timestamp} -- {Message}"

            if (log.HasAttribute(LogAttribute.Function))
            {
                var function = log.GetAttribute<object>(LogAttribute.Function);

                var printedFunctions = (HashSet<object>)state;
                if (printedFunctions.Add(function))
                {
                    textWriter.WriteLine(function);
                }
            }
            else if (log.IsValid)
            {
                textWriter.WriteLine("Log is missing Function attribute: {0} - {1}", log.Timestamp, log.Message);
            }
        }

        private static object PrintModeStateGenDifferentFunctionOnly() => new object[1];

        private static void PrintModeDifferentFunctionOnly(TextWriter textWriter, ILogEntry log, object state)
        {
            // Equiv: @{Function}$!"@vLog is missing Function attribute: {Timestamp} -- {Message}"

            if (log.HasAttribute(LogAttribute.Function))
            {
                var function = log.GetAttribute<object>(LogAttribute.Function);

                var lastFunction = (object[])state;
                if (lastFunction[0] == null || !lastFunction[0].Equals(function))
                {
                    textWriter.WriteLine(function);
                    lastFunction[0] = function;
                }
            }
            else if (log.IsValid)
            {
                textWriter.WriteLine("Log is missing Function attribute: {0} - {1}", log.Timestamp, log.Message);
            }
        }

        #endregion

        /// <summary>
        /// Helper function that prints each log until done or canceled.
        /// </summary>
        /// <param name="printer">The log printer. First is the text writer, second parameter is the log, the third is a state object</param>
        /// <param name="printerStateGenerator">State generator.</param>
        /// <param name="cancellationToken">Print-process cancellation token.</param>
        protected void PrintHelper(Action<TextWriter, ILogEntry, object> printer, Func<object> printerStateGenerator, CancellationToken cancellationToken)
        {
            var observableRunningTokenSource = new CancellationTokenSource();

            var state = printerStateGenerator();
            var dis = logRegistry.Logs.TakeWhile(_ => !cancellationToken.IsCancellationRequested).Subscribe(log => printer(TextWriter, log, state), () =>
            {
                observableRunningTokenSource.Cancel();
            });

            while (!cancellationToken.IsCancellationRequested && !observableRunningTokenSource.IsCancellationRequested)
            {
                Thread.Sleep(100);
            }

            dis.Dispose();
        }
        
        /// <summary>
        /// Print the logs contained with the registry (async).
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        /// <remarks>This is too simplistic and will be replaced or augmented at some point in the future. Don't build logic around a simple Print call.</remarks>
        public override Task PrintAsync(CancellationToken cancellationToken)
        {
            printCounter.Increment();

            //return Task.Run(() => PrintThreadTraces()); //TODO

            return Task.Run(() => LogPrintHandler?.Invoke(cancellationToken), cancellationToken);
        }

        private void SetPrintHandler(Action<TextWriter, ILogEntry, object> printer, Func<object> printerStateGenerator)
        {
            LogPrintHandler = ct => PrintHelper(printer, printerStateGenerator, ct);
        }

        private void ParsePrintMode(string mode)
        {
            //XXX should an error be printed if something here is wrong?
            if (!string.IsNullOrWhiteSpace(mode) && (char.IsLetter(mode[0]) || mode[0] == '@'))
            {
                if (mode[0] == '@')
                {
                    var printMode = new PrintModeFormat();
                    printMode.SetMode(mode, this);
                    printMode.UpdateDirectWrite();
                    SetPrintHandler(printMode.Process, printMode.GenerateStateObject);
                }
                else if (PrintHandlers.ContainsKey(mode.ToLower()))
                {
                    var (printFunc, printFuncStateGen) = PrintHandlers[mode.ToLower()];
                    SetPrintHandler(printFunc, printFuncStateGen);
                }
                else
                {
                    throw new ArgumentException($"Unknwon printMode: {mode}");
                }
            }
        }

        /// <summary>
        /// Set a config for the printer.
        /// </summary>
        /// <param name="config">The config for the printer.</param>
        public virtual void SetConfig(LogConfig config)
        {
            if (config.ExtraConfigs != null && config.ExtraConfigs.ContainsKey("printMode"))
            {
                ParsePrintMode(config.ExtraConfigs["printMode"]);
            }
            if (LogPrintHandler == null)
            {
                ParsePrintMode(DefaultPrinterName);
            }
        }

        /// <summary>
        /// Set the registry to get logs from.
        /// </summary>
        /// <param name="registry">The registry to get logs from.</param>
        public void SetRegistry(ILogRegistry registry)
        {
            logRegistry = registry;
        }
    }
}
