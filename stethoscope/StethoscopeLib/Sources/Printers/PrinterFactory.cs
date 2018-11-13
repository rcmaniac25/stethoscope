using Metrics;

using Stethoscope.Common;
using Stethoscope.Printers.Internal;

namespace Stethoscope.Printers
{
    /// <summary>
    /// Meta Factory object for picking a log printer.
    /// </summary>
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

        /// <summary>
        /// Create a console-printer factory.
        /// </summary>
        /// <returns>Printer factory.</returns>
        public static IPrinterFactory CrateConsoleFactory()
        {
            factoryCreationCounter.Increment("console");

            return new ConsolePrinterFactory();
        }

        private class ConsolePrinterFactory : IPrinterFactory
        {
            public IPrinter Create(ILogRegistry registry, LogConfig config)
            {
                creationCounter.Increment("console");

                var printer = new ConsolePrinter();
                printer.SetRegistry(registry);
                printer.SetConfig(config);
                return printer;
            }
        }

        /// <summary>
        /// Create a file-printing factory.
        /// </summary>
        /// <param name="defaultPath">The default path to use if not specified in config</param>
        /// <returns>Printer factory.</returns>
        public static IPrinterFactory CrateFileFactory(string defaultPath = "")
        {
            factoryCreationCounter.Increment("file");

            return new FilePrinterFactory(defaultPath);
        }

        private class FilePrinterFactory : IPrinterFactory
        {
            private string defaultPath;

            public FilePrinterFactory(string def)
            {
                defaultPath = def;
            }

            public IPrinter Create(ILogRegistry registry, LogConfig config)
            {
                creationCounter.Increment("file");

                var printer = new FilePrinter(defaultPath);
                printer.SetRegistry(registry);
                printer.SetConfig(config);
                return printer;
            }
        }
    }
}
