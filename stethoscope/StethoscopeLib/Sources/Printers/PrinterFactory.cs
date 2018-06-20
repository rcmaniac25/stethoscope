using Metrics;

using Stethoscope.Common;
using Stethoscope.Printers.Internal;

namespace Stethoscope.Printers
{
    public class PrinterFactory
    {
        private static readonly Counter printerFactoryCreationCounter;
        private static readonly Counter printerCreationCounter;

        static PrinterFactory()
        {
            var printerContext = Metric.Context("Printer Factory");
            printerFactoryCreationCounter = printerContext.Counter("Creation", Unit.Calls, "printer, factory");
            printerCreationCounter = printerContext.Counter("Usage", Unit.Calls, "printer");
        }

        private PrinterFactory()
        {
        }

        public static IPrinterFactory CrateConsoleFactory()
        {
            printerFactoryCreationCounter.Increment();

            return new ConsolePrinterFactory();
        }

        private class ConsolePrinterFactory : IPrinterFactory
        {
            public IPrinter Create(ILogRegistry registry, LogConfig config)
            {
                printerCreationCounter.Increment();

                var printer = new ConsolePrinter();
                printer.SetRegistry(registry);
                printer.SetConfig(config);
                return printer;
            }
        }
    }
}
