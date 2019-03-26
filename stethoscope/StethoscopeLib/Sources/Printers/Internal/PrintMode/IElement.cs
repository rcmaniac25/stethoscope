﻿using Stethoscope.Common;

using System.IO;

namespace Stethoscope.Printers.Internal.PrintMode
{
    /// <summary>
    /// An individual print mode element.
    /// </summary>
    public interface IElement
    {
        /// <summary>
        /// Generate the state values necessary for <see cref="Process(TextWriter, ILogEntry, object)"/> to run.
        /// </summary>
        /// <returns>Generated state object.</returns>
        object GenerateStateObject();

        /// <summary>
        /// Process an individual log with the stored print mode values.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="log">The log to process.</param>
        /// <param name="state">The state generated by <see cref="GenerateStateObject"/></param>
        void Process(TextWriter writer, ILogEntry log, object state);

        /// <summary>
        /// Get the error handler that exists for this element, if one exists
        /// </summary>
        IExceptionHandler ExceptionHandler { get; }
    }
}