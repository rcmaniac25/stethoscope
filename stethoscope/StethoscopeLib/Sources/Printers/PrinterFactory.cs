using Stethoscope.Common;
using Stethoscope.Printers.Internal;

namespace Stethoscope.Printers
{
    public class PrinterFactory
    {
        private PrinterFactory()
        {
        }

        public static IPrinterFactory CrateConsoleFactory() => new ConsolePrinterFactory(); //TODO: record stat about function used

        private class ConsolePrinterFactory : IPrinterFactory
        {
            public IPrinter Create(ILogRegistry registry, LogConfig config)
            {
                //TODO: record stat about function used (maybe config values?)
                var printer = new ConsolePrinter();
                printer.SetRegistry(registry);
                printer.SetConfig(config);
                return printer;
            }
        }
    }
}
