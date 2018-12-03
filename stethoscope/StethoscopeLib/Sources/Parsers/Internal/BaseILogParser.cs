using Stethoscope.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Stethoscope.Parsers.Internal
{
    //TODO: C# 8 default impl. for interfaces will be used, so this can be removed

    /// <summary>
    /// Provide defaults for ILogParser.
    /// Temporary helper class until C# 8 default impl. is available
    /// </summary>
    public abstract class BaseILogParser : ILogParser
    {
        /// <summary>
        /// Parse (sync) a stream of data to get applicable logs.
        /// </summary>
        /// <param name="logStream">The stream of log data.</param>
        public void Parse(Stream logStream)
        {
            ParseAsync(logStream).Wait();
        }

        /// <summary>
        /// Parse (async) a stream of data to get applicable logs.
        /// </summary>
        /// <param name="logStream">The stream of log data.</param>
        /// <returns>Task representing the parse operation.</returns>
        public Task ParseAsync(Stream logStream)
        {
            return ParseAsync(logStream, new CancellationToken());
        }

        /// <summary>
        /// Parse (async) a stream of data to get applicable logs.
        /// </summary>
        /// <param name="logStream">The stream of log data.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        /// <returns>Task representing the parse operation.</returns>
        public abstract Task ParseAsync(Stream logStream, CancellationToken cancellationToken);

        /// <summary>
        /// Apply additional context to the parser, using specific configs to modify parsing.
        /// </summary>
        /// <param name="config">A collection of configs to modify the parser with.</param>
        /// <param name="context">The context that the modified parser will execute in. If the scope of this parser is exited, as in the Action delegate finishes execution, then the modified parser becomes invalid and won't run.</param>
        public abstract void ApplyContextConfig(IDictionary<ContextConfigs, object> config, Action<ILogParser> context);
    }
}
