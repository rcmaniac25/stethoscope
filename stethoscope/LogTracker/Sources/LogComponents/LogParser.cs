using LogTracker.Common;
using LogTracker.Parsers.XML;
using LogTracker.Printers;

namespace LogTracker.Log
{
    public class LogParser
    {
        private ILogParser logParser;
        private IPrinter printer;
        private LogRegistry registry;

        public LogParser(LogConfig config)
        {
            registry = new LogRegistry();

            logParser = new XMLLogParser();
            logParser.SetConfig(config);
            logParser.SetRegistry(registry);

            printer = new ConsolePrinter();
            printer.SetConfig(config);
            printer.SetRegistry(registry);
        }
        
        public void Process(string logFile)
        {
            logParser.Parse(logFile);
        }

        public void PrintTrace()
        {
            printer.Setup();

            printer.Print();

            printer.Teardown();
        }
    }
}
