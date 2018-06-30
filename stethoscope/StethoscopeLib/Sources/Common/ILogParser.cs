using System;
using System.Collections.Generic;
using System.IO;

namespace Stethoscope.Common
{
    /// <summary>
    /// A log parser.
    /// </summary>
    public interface ILogParser
    {
        /// <summary>
        /// Parse a stream of data to get applicable logs.
        /// </summary>
        /// <param name="logStream">The stream of log data.</param>
        void Parse(Stream logStream);

        /// <summary>
        /// Apply additional context to the parser, using specific configs to modify parsing.
        /// </summary>
        /// <param name="config">A collection of configs to modify the parser with.</param>
        /// <param name="context">The context that the modified parser will execute in. If the scope of this parser is exited, as in the Action delegate finishes execution, then the modified parser becomes invalid and won't run.</param>
        void ApplyContextConfig(IDictionary<ContextConfigs, object> config, Action<ILogParser> context);
    }
}
