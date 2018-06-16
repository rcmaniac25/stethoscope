using Stethoscope.Common;

using System;
using System.IO;
using System.Reactive.Linq;

namespace Stethoscope.Printers.Internal
{
    public abstract class IOPrinter : IPrinter
    {
        protected ILogRegistry logRegistry;

        protected TextWriter TextWriter { get; set; }

        public abstract void Setup();
        public abstract void Teardown();

        protected static string GenerateIndentLog(int indent) => new string(' ', indent * 2); //XXX config for indent size

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

        public virtual void Print() => PrintThreadTraces(); //TODO: record stat about function used

        public void SetConfig(LogConfig config)
        {
        }

        public void SetRegistry(ILogRegistry registry)
        {
            logRegistry = registry;
        }
    }
}
