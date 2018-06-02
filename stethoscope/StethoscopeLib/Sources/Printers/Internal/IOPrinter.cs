using Stethoscope.Common;

using System.IO;
using System.Linq;

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
            var threads = logRegistry.GetBy(LogAttribute.ThreadID);
            foreach (var thread in threads)
            {
                int indent = 0;
                TextWriter.WriteLine($"Thread {thread.Key}");

                var firstAttribute = thread.Value.First();
                var initSrc = firstAttribute.GetAttribute<string>(LogAttribute.SourceFile);
                var lastFunc = firstAttribute.GetAttribute<string>(LogAttribute.Function);

                var lastFuncSrc = $"{initSrc}.{lastFunc}";

                TextWriter.WriteLine($"Start {lastFunc} // {initSrc}");
                foreach (var log in thread.Value)
                {
                    var src = log.GetAttribute<string>(LogAttribute.SourceFile);
                    var function = log.GetAttribute<string>(LogAttribute.Function);
                    var funcSrc = $"{src}.{function}";

                    if (funcSrc != lastFuncSrc)
                    {
                        TextWriter.WriteLine($"End {lastFunc}");
                        TextWriter.WriteLine($"Start {function} // {src}");

                        lastFuncSrc = funcSrc;
                        lastFunc = function;
                    }

                    TextWriter.WriteLine($"{GenerateIndentLog(indent + 1)}{log.Message}");
                }

                TextWriter.WriteLine($"End {lastFunc}");
                TextWriter.WriteLine();
            }
        }

        public virtual void Print() => PrintThreadTraces();

        public void SetConfig(LogConfig config)
        {
        }

        public void SetRegistry(ILogRegistry registry)
        {
            logRegistry = registry;
        }
    }
}
