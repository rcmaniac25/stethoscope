using System;

namespace Stethoscope.Printers.Internal.PrintMode
{
    /// <summary>
    /// Print exception.
    /// </summary>
    public class PrintException : Exception
    {
        /// <summary>
        /// Create a new PrintException.
        /// </summary>
        /// <param name="message">The message stating what happened.</param>
        public PrintException(string message) : base(message)
        {
        }
    }
}
