using System;
using System.Collections.Generic;
using System.IO;

namespace Stethoscope.Common
{
    public static class LogParserExtensions
    {
        public static void Parse(this ILogParser parser, string logFile)
        {
            //TODO: record stat about function used
            using (var fr = new FileStream(logFile, FileMode.Open))
            {
                //TODO: record stat about file size
                parser.Parse(fr);
            }
        }

        public static void ApplyContextConfig(this ILogParser parser, ContextConfigs configName, object configValue, Action<ILogParser> context)
        {
            //TODO: record stat about function and config name used
            var config = new Dictionary<ContextConfigs, object>
            {
                { configName, configValue }
            };
            parser.ApplyContextConfig(config, context);
        }
    }
}
