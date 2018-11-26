using Metrics;

using Stethoscope.Common;

using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Stethoscope.Printers.Internal
{
    /// <summary>
    /// Log printer that prints to an IO object of type <see cref="TextReader"/>.
    /// </summary>
    public abstract class IOPrinter : IPrinter
    {
        /// <summary>
        /// Metric counter for indicating every time <see cref="Print"/> is invoked.
        /// </summary>
        protected static readonly Counter printCounter = Metric.Counter("IO Printer Print", Unit.Calls, "IO, printer");

        /// <summary>
        /// The log registry that logs will be retrieved from.
        /// </summary>
        protected ILogRegistry logRegistry;

        /// <summary>
        /// The TextWriter which will be printed to.
        /// </summary>
        protected TextWriter TextWriter { get; set; }

        /// <summary>
        /// Setup the printer.
        /// </summary>
        public abstract void Setup();
        /// <summary>
        /// Teardown the printer.
        /// </summary>
        public abstract void Teardown();

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

        /// <summary>
        /// Print the logs contained with the registry (sync).
        /// </summary>
        /// <remarks>This is too simplistic and will be replaced or augmented at some point in the future. Don't build logic around a simple Print call.</remarks>
        public virtual void Print()
        {
            PrintAsync().Wait();
        }

        /// <summary>
        /// Print the logs contained with the registry (async).
        /// </summary>
        /// <remarks>This is too simplistic and will be replaced or augmented at some point in the future. Don't build logic around a simple Print call.</remarks>
        public virtual Task PrintAsync()
        {
            printCounter.Increment();

            return Task.Run(() => PrintThreadTraces()); //TODO
        }

        /// <summary>
        /// Set a config for the printer.
        /// </summary>
        /// <param name="config">The config for the printer.</param>
        public virtual void SetConfig(LogConfig config)
        {
            if (config.UserConfigs != null && config.UserConfigs.ContainsKey("PrintMode"))
            {
                //TODO
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
