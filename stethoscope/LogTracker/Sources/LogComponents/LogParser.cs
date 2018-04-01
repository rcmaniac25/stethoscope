using LogTracker.Common;

namespace LogTracker.Log
{
    public class LogParser
    {
        private ILogParser logParser;
        private IPrinter printer;
        private LogRegistry registry;

        public LogParser(LogConfig config, ILogParserFactory parserFactory, IPrinterFactory printerFactory)
        {
            registry = new LogRegistry();
            
            logParser = parserFactory.Create(registry, config);
            printer = printerFactory.Create(registry, config);
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
