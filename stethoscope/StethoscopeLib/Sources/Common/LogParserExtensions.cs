using Metrics;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Stethoscope.Common
{
    /// <summary>
    /// Extensions for <see cref="ILogParser"/>
    /// </summary>
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

        /// <summary>
        /// Parse a log file.
        /// </summary>
        /// <param name="parser">The parser to use.</param>
        /// <param name="logFile">Path to a log file.</param>
        public static void Parse(this ILogParser parser, string logFile)
        {
            parseCounter.Increment();

            using (var fr = new FileStream(logFile, FileMode.Open))
            {
                parseFileSizeHistogram.Update(fr.Length, logFile);

                parser.Parse(fr);
            }
        }

        /// <summary>
        /// Parse a log file.
        /// </summary>
        /// <param name="parser">The parser to use.</param>
        /// <param name="logFile">Path to a log file.</param>
        /// <returns>Task representing the parse operation.</returns>
        public static Task ParseAsync(this ILogParser parser, string logFile)
        {
            parseCounter.Increment();

            var fr = new FileStream(logFile, FileMode.Open);
            parseFileSizeHistogram.Update(fr.Length, logFile);

            return parser.ParseAsync(fr).ContinueWith(_ => fr.Dispose(), TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Parse a log file.
        /// </summary>
        /// <param name="parser">The parser to use.</param>
        /// <param name="logFile">Path to a log file.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        /// <returns>Task representing the parse operation.</returns>
        public static Task ParseAsync(this ILogParser parser, string logFile, CancellationToken cancellationToken)
        {
            parseCounter.Increment();

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            var fr = new FileStream(logFile, FileMode.Open);
            parseFileSizeHistogram.Update(fr.Length, logFile);

            return parser.ParseAsync(fr, cancellationToken).ContinueWith(_ => fr.Dispose(), TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Apply additional context to the parser, using a specific config to modify parsing.
        /// </summary>
        /// <param name="parser">The parser to apply context to.</param>
        /// <param name="configName">The specific config to apply.</param>
        /// <param name="configValue">The value associated with the specified config.</param>
        /// <param name="context">The context that the modified parser will execute in. If the scope of this parser is exited, as in the Action delegate finishes execution, then the modified parser becomes invalid and won't run.</param>
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
