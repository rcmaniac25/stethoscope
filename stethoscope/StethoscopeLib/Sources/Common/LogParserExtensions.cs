using Metrics;

using System;
using System.Collections.Generic;
using System.IO;

namespace Stethoscope.Common
{
    public static class LogParserExtensions
    {
        private static readonly Counter parseCounter;
        private static readonly Histogram parseFileSizeHistogram;
        private static readonly Counter applyContextCounter;

        static LogParserExtensions()
        {
            var logParserExtContext = Metric.Context("LogParser Extensions");
            parseCounter = logParserExtContext.Counter("Parse", Unit.Calls, "log, parser, parse");
            parseFileSizeHistogram = logParserExtContext.Histogram("Parse File Size", Unit.Bytes, SamplingType.Default, "log, parser, parse, file");
            applyContextCounter = logParserExtContext.Counter("ApplyContext", Unit.Calls, "log, parser, applycontext");
        }

        public static void Parse(this ILogParser parser, string logFile)
        {
            parseCounter.Increment();

            using (var fr = new FileStream(logFile, FileMode.Open))
            {
                parseFileSizeHistogram.Update(fr.Length, logFile);

                parser.Parse(fr);
            }
        }

        public static void ApplyContextConfig(this ILogParser parser, ContextConfigs configName, object configValue, Action<ILogParser> context)
        {
            applyContextCounter.Increment(configName.ToString());

            var config = new Dictionary<ContextConfigs, object>
            {
                { configName, configValue }
            };
            parser.ApplyContextConfig(config, context);
        }
    }
}
