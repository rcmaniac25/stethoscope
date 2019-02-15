﻿using Stethoscope.Common;

using System;
using System.Collections.Generic;
using System.IO;

namespace Stethoscope.Printers.Internal.PrintMode
{
    /// <summary>
    /// Ordered list of processaable print mode values.
    /// </summary>
    public class PrintModeFormat : IElement
    {
        private IConditional logConditional;

        private List<IElement> elements = new List<IElement>();

        //TODO

        /// <summary>
        /// When processing a log, write directly to the specified writer.
        /// If enabled, it may result in some logs being partially written out, depending on modifiers.
        /// </summary>
        public bool DirectWrite { get; set; }

        /// <summary>
        /// Set the print mode value to use. This will reset any existing print mode values.
        /// </summary>
        /// <param name="mode">The print mode to use.</param>
        /// <param name="printer">The printer that will be using the print mode.</param>
        public void SetMode(string mode, IPrinter printer)
        {
            if (mode == null)
            {
                throw new ArgumentNullException(nameof(mode));
            }

            logConditional = null;
            elements.Clear();

            var factory = printer.ElementFactory ?? new ElementFactor();
            //TODO
        }

        /// <summary>
        /// Evaluate the the format that has been set to determine if <see cref="DirectWrite"/> can be used.
        /// </summary>
        public void UpdateDirectWrite()
        {
#if false
            DirectWrite = true;
            foreach (var element in elements)
            {
                //TODO: check for failure handlers and what might happen
            }
#endif
        }

        /// <summary>
        /// Process an individual log with the stored print mode values.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="log">The log to process.</param>
        /// <param name="state">The state generated by <see cref="GenerateStateObject"/></param>
        public void Process(TextWriter writer, ILogEntry log, object state)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            var innerState = (object[])state;
            var innerStateCurrentIndex = logConditional == null ? 0 : 1;
            if (innerState.Length != (elements.Count + innerStateCurrentIndex))
            {
                throw new InvalidOperationException("State is not compatible with this PrintMode. The format may have changed between GenerateStateObject and Process.");
            }

            var directWrite = DirectWrite; // Precaution if value is changed mid-write. Weird, but is plausible and cheap to handle
            var logWriter = directWrite ? writer : new StringWriter() { NewLine = writer.NewLine };

            /* Steps:
             * 1. evaluate log-level conditional
             * 2. Iterate over all format parts
             * 3. Execute part (writing occurs to a buffer)
             *     a. If raw, print. If not raw, evaluate conditional
             *     b. if conditional passes, print value (using modifiers if need-be) and notify conditional
             *     c. FUTURE-TODO: if conditional fails, and a failed conditional modifier exists, invoke it
             *     d. if exception occurs, invoke failure handler modifier if it exists
             *  4. Flush buffer to actual text writer
             */
            
            // #1
            if (logConditional != null)
            {
                if (!logConditional.ShouldProcess(log, innerState[0]))
                {
                    return;
                }
            }

            // #2
            foreach (var element in elements)
            {
                // #3
                //XXX error handling?
                element.Process(logWriter, log, innerState[innerStateCurrentIndex++]);
            }

            // #4
            if (!directWrite)
            {
                writer.Write(((StringWriter)logWriter).ToString());
                writer.Flush();
            }
        }

        /// <summary>
        /// Generate the state values necessary for <see cref="Process(TextWriter, ILogEntry, object)"/> to run.
        /// </summary>
        /// <returns>Generated state object.</returns>
        public object GenerateStateObject()
        {
            var index = logConditional == null ? 0 : 1;
            var state = new object[elements.Count + index];

            if (logConditional != null)
            {
                state[0] = logConditional.GenerateState();
            }
            foreach (var element in elements)
            {
                state[index++] = element.GenerateStateObject();
            }

            return state;
        }
    }
}
