using Stethoscope.Common;

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
        /// Create an "attribute" print element.
        /// </summary>
        /// <param name="attribute">The element that will be printed.</param>
        /// <param name="attributeFormat">The <see cref="string.Format(string, object)"/> style print to print the attribute out with. Default value only prints the attribute.</param>
        /// <param name="conditionals">All conditions used to determine if the element should be printed.</param>
        /// <param name="modifiers">All modifiers to apply to the printed element.</param>
        /// <returns>The created element.</returns>
        public IElement CreateElement(LogAttribute attribute, string attributeFormat = StandardElement.DefaultAttributeFormat, IConditional[] conditionals = null, IModifier[] modifiers = null)
        {
            if (attributeFormat == null)
            {
                throw new ArgumentNullException(nameof(attributeFormat));
            }
            var element = new StandardElement(attribute);
            if (attributeFormat != StandardElement.DefaultAttributeFormat || 
                (conditionals != null && conditionals.Length > 0) || 
                (modifiers != null && modifiers.Length > 0))
            {
                element.OptionalInitialize(attributeFormat, conditionals, modifiers);
            }
            return element;
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
