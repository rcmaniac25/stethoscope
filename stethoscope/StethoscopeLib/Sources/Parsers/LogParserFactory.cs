﻿using Stethoscope.Common;
using Stethoscope.Parsers.Internal.XML;

namespace Stethoscope.Parsers
{
    public class LogParserFactory
    {
        private LogParserFactory()
        {
        }

        public static ILogParserFactory GetParserForFileExtension(string ext)
        {
            //TODO: record stat about function used and ext
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
                //TODO: record stat about function used (maybe config values?)
                var parser = new XMLLogParser();
                parser.SetRegistry(registry);
                parser.SetConfig(config);
                return parser;
            }
        }
    }
}
