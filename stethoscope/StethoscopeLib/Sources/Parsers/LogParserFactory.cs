using Metrics;

using Stethoscope.Common;
using Stethoscope.Parsers.Internal.XML;

namespace Stethoscope.Parsers
{
    public class LogParserFactory
    {
        private static readonly Counter logParserFactoryCreationCounter;
        private static readonly Counter logParserCreationCounter;

        static LogParserFactory()
        {
            var printerContext = Metric.Context("LogParser Factory");
            logParserFactoryCreationCounter = printerContext.Counter("Creation", Unit.Calls, "log, parser, factory");
            logParserCreationCounter = printerContext.Counter("Usage", Unit.Calls, "log, parser");
        }

        private LogParserFactory()
        {
        }

        public static ILogParserFactory GetParserForFileExtension(string ext)
        {
            logParserFactoryCreationCounter.Increment(ext);

            switch (ext.ToLower())
            {
                case "xml":
                    return new XMLParserFactory();
            }
            return null;
        }

        private class XMLParserFactory : ILogParserFactory
        {
            public ILogParser Create(ILogRegistry registry, LogConfig config)
            {
                logParserCreationCounter.Increment();
                
                var parser = new XMLLogParser();
                parser.SetRegistry(registry);
                parser.SetConfig(config);
                return parser;
            }
        }
    }
}
