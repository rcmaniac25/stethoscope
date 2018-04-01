using LogTracker.Common;

namespace LogTracker.Printers
{
    public class PrinterFactory
    {
        private PrinterFactory()
        {
        }

        public static IPrinterFactory CrateConsoleFactory()
        {
            return new ConsolePrinterFactory();
        }

        private class ConsolePrinterFactory : IPrinterFactory
        {
            public IPrinter Create(ILogRegistry registry, LogConfig config)
            {
                var printer = new ConsolePrinter();
                printer.SetRegistry(registry);
                printer.SetConfig(config);
                return printer;
            }
        }
    }
}
