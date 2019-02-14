using System;

namespace Stethoscope.Printers.Internal.PrintMode
{
    /// <summary>
    /// Default print mode element factory.
    /// </summary>
    public class ElementFactor : IPrinterElementFactory
    {
        /// <summary>
        /// Create a print mode conditional element.
        /// </summary>
        /// <param name="element">The element to create.</param>
        /// <returns>The created conditional.</returns>
        public virtual IConditional CreateConditional(ConditionalElement element)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a print mode modifier element.
        /// </summary>
        /// <param name="element">The element to create.</param>
        /// <returns>The created modifier.</returns>
        public virtual IModifier CreateModifier(ModifierElement element)
        {
            throw new NotImplementedException();
        }
    }
}
