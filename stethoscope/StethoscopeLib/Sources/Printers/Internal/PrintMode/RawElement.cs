using Stethoscope.Common;

using System.IO;

namespace Stethoscope.Printers.Internal.PrintMode
{
    /// <summary>
    /// Raw text element.
    /// </summary>
    public class RawElement : IElement
    {
        private readonly string text;

        /// <summary>
        /// Create a new raw text element.
        /// </summary>
        /// <param name="text">The text to print.</param>
        public RawElement(string text)
        {
            this.text = text;
        }

        /// <summary>
        /// Unused
        /// </summary>
        public IExceptionHandler ExceptionHandler => null;

        /// <summary>
        /// No-op
        /// </summary>
        /// <returns>null</returns>
        public object GenerateStateObject()
        {
            return null;
        }

        /// <summary>
        /// Print a specific string of text to the <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">Writer to print to.</param>
        /// <param name="log">Unused</param>
        /// <param name="state">Unused</param>
        public void Process(TextWriter writer, ILogEntry log, object state)
        {
            writer.Write(text);
        }
    }
}
