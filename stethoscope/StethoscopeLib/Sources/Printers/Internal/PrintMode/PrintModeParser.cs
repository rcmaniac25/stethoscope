using Stateless;

using System;
using System.Collections.Generic;
using System.Text;

namespace Stethoscope.Printers.Internal.PrintMode
{
    /// <summary>
    /// Parser for print mode formats. Not to be confused with the printable type, <see cref="PrintModeFormat"/>.
    /// </summary>
    public class PrintModeParser
    {
        private List<IElement> elements = new List<IElement>();

        private PrintModeParser()
        {
        }

        //XXX temp until enumerator functions added directly (IList<IElement>)
        public IList<IElement> TempInternalElements => elements;

        /// <summary>
        /// Get any conditional that applies to the entire parsed string.
        /// </summary>
        public IConditional GlobalConditional => null; //TODO

        /// <summary>
        /// Parse a print mode format and add to self for storage.
        /// </summary>
        /// <param name="format">The print mode format to parse.</param>
        /// <param name="factory">The element factory to generate elements after parsing.</param>
        /// <returns>The parsed format.</returns>
        public static PrintModeParser Parse(string format, IPrinterElementFactory factory)
        {
            //TODO
            return new PrintModeParser(); //XXX
        }
    }
}
