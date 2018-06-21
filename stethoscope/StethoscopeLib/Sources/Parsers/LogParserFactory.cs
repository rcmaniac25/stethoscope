﻿using Metrics;

using Stethoscope.Common;
using Stethoscope.Parsers.Internal.XML;

namespace Stethoscope.Parsers
{
    public class LogParserFactory
    {
        private static readonly Counter factoryCreationCounter;
        private static readonly Counter creationCounter;

        static LogParserFactory()
        {
            var printerContext = Metric.Context("LogParser Factory");
            factoryCreationCounter = printerContext.Counter("Creation", Unit.Calls, "log, parser, factory");
            creationCounter = printerContext.Counter("Usage", Unit.Calls, "log, parser");
        }

        private LogParserFactory()
        {
        }

        public static ILogParserFactory GetParserForFileExtension(string ext)
        {
            factoryCreationCounter.Increment(ext);

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
                creationCounter.Increment();
                
                var parser = new XMLLogParser();
                parser.SetRegistry(registry);
                parser.SetConfig(config);
                return parser;
            }
        }
    }
}
