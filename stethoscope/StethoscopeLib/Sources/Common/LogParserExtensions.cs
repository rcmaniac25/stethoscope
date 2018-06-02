using System;
using System.Collections.Generic;
using System.IO;

namespace Stethoscope.Common
{
    public static class LogParserExtensions
    {
        public static void Parse(this ILogParser parser, string logFile)
        {
            using (var fr = new FileStream(logFile, FileMode.Open))
            {
                parser.Parse(fr);
            }
        }

        public static void ApplyContextConfig(this ILogParser parser, ContextConfigs configName, object configValue, Action<ILogParser> context)
        {
            var config = new Dictionary<ContextConfigs, object>
            {
                { configName, configValue }
            };
            parser.ApplyContextConfig(config, context);
        }
    }
}
