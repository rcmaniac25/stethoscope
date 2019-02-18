using Stethoscope.Common;
using Stethoscope.Printers.Internal.PrintMode;

namespace Stethoscope.Printers.Internal
{
    /// <summary>
    /// Modifier elements that can be created.
    /// </summary>
    public enum ModifierElement
    {
        /// <summary>
        /// Error handler modifier. Will only be invoked if an error occurs during printing.
        /// </summary>
        ErrorHandler
    }

    /// <summary>
    /// Conditional elements that can be created.
    /// </summary>
    public enum ConditionalElement
    {
        /// <summary>
        /// Process only if specified attribute exists.
        /// </summary>
        AttributeExists,
        /// <summary>
        /// Process only if the specific attribute value is different from the prior attribute value (or is new)
        /// </summary>
        AttributeValueChanged,
        /// <summary>
        /// Process only if the specific attribute value hasn't been seen before.
        /// </summary>
        AttributeValueNew,
        /// <summary>
        /// Process only if the log is valid. <see cref="ILogEntry.IsValid"/> = <c>true</c>
        /// </summary>
        ValidLog,
        /// <summary>
        /// Process only if the log is invalid. <see cref="ILogEntry.IsValid"/> = <c>false</c>
        /// </summary>
        InvalidLog
    }
    
    /// <summary>
    /// Factory for creating elements used for print mode formats.
    /// </summary>
    public interface IPrinterElementFactory
    {
        /// <summary>
        /// Create a "raw" print element.
        /// </summary>
        /// <param name="text">The text that will be printed when the element is processed.</param>
        /// <returns>The created element.</returns>
        IElement CreateRaw(string text);

        /// <summary>
        /// Create an "attribute" print element.
        /// </summary>
        /// <param name="attribute">The element that will be printed.</param>
        /// <param name="attributeFormat">The <see cref="string.Format(string, object)"/> style print to print the attribute out with. Default value only prints the attribute.</param>
        /// <param name="conditionals">All conditions used to determine if the element should be printed.</param>
        /// <param name="modifiers">All modifiers to apply to the printed element.</param>
        /// <returns>The created element.</returns>
        IElement CreateElement(LogAttribute attribute, string attributeFormat = "{0}", IConditional[] conditionals = null, IModifier[] modifiers = null);

        /// <summary>
        /// Create a print mode modifier element.
        /// </summary>
        /// <param name="element">The element to create.</param>
        /// <returns>The created modifier.</returns>
        IModifier CreateModifier(ModifierElement element);

        /// <summary>
        /// Create a print mode conditional element.
        /// </summary>
        /// <param name="element">The element to create.</param>
        /// <returns>The created conditional.</returns>
        IConditional CreateConditional(ConditionalElement element);
    }
}
