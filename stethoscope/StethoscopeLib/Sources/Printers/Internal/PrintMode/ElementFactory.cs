using System;

namespace Stethoscope.Printers.Internal.PrintMode
{
    /// <summary>
    /// Default print mode element factory.
    /// </summary>
    public class ElementFactory : IPrinterElementFactory
    {
        /// <summary>
        /// Create a "raw" print element.
        /// </summary>
        /// <param name="text">The text that will be printed when the element is processed.</param>
        /// <returns>The created element.</returns>
        public virtual IElement CreateRaw(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (text == null)
                {
                    throw new ArgumentNullException(nameof(text));
                }
                throw new ArgumentException("text cannot be empty", nameof(text));
            }
            return new RawElement(text);
        }

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
