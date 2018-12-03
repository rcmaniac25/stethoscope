﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Stethoscope.Common
{
    /// <summary>
    /// A log parser.
    /// </summary>
    public interface ILogParser
    {
        /// <summary>
        /// Parse (sync) a stream of data to get applicable logs.
        /// </summary>
        /// <param name="logStream">The stream of log data.</param>
        void Parse(Stream logStream); //TODO: C# 8 default impl. for interfaces

        /// <summary>
        /// Parse (async) a stream of data to get applicable logs.
        /// </summary>
        /// <param name="logStream">The stream of log data.</param>
        /// <returns>Task representing the parse operation.</returns>
        Task ParseAsync(Stream logStream); //TODO: C# 8 default impl. for interfaces

        /// <summary>
        /// Parse (async) a stream of data to get applicable logs.
        /// </summary>
        /// <param name="logStream">The stream of log data.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        /// <returns>Task representing the parse operation.</returns>
        Task ParseAsync(Stream logStream, CancellationToken cancellationToken);

        /// <summary>
        /// Apply additional context to the parser, using specific configs to modify parsing.
        /// </summary>
        /// <param name="config">A collection of configs to modify the parser with.</param>
        /// <param name="context">The context that the modified parser will execute in. If the scope of this parser is exited, as in the Action delegate finishes execution, then the modified parser becomes invalid and won't run.</param>
        void ApplyContextConfig(IDictionary<ContextConfigs, object> config, Action<ILogParser> context);
    }
}
