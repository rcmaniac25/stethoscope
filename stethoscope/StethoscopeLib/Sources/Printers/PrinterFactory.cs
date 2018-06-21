using Metrics;

using Stethoscope.Common;
using Stethoscope.Printers.Internal;

namespace Stethoscope.Printers
{
    public class PrinterFactory
    {
        private static readonly Counter factoryCreationCounter;
        private static readonly Counter creationCounter;

        static PrinterFactory()
        {
            var printerContext = Metric.Context("Printer Factory");
            factoryCreationCounter = printerContext.Counter("Creation", Unit.Calls, "printer, factory");
            creationCounter = printerContext.Counter("Usage", Unit.Calls, "printer");
        }

        private PrinterFactory()
        {
        }

        public static IPrinterFactory CrateConsoleFactory()
        {
            factoryCreationCounter.Increment();

            return new ConsolePrinterFactory();
        }

        private class ConsolePrinterFactory : IPrinterFactory
        {
            public IPrinter Create(ILogRegistry registry, LogConfig config)
            {
                creationCounter.Increment();

                var printer = new ConsolePrinter();
                printer.SetRegistry(registry);
                printer.SetConfig(config);
                return printer;
            }
        }
    }
}
