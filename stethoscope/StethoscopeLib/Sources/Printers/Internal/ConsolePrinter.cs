using System;

namespace Stethoscope.Printers.Internal
{
    /// <summary>
    /// Log printer that prints to the standard out.
    /// </summary>
    public class ConsolePrinter : IOPrinter
    {
        /// <summary>
        /// Setup the printer.
        /// </summary>
        public override void Setup()
        {
            TextWriter = Console.Out;
        }

        /// <summary>
        /// Teardown the printer.
        /// </summary>
        public override void Teardown()
        {
        }
    }
}
