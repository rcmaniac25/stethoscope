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
        /// Process only if the log is valid. <see cref="Stethoscope.Common.ILogEntry.IsValid"/> = <c>true</c>
        /// </summary>
        ValidLog,
        /// <summary>
        /// Process only if the log is invalid. <see cref="Stethoscope.Common.ILogEntry.IsValid"/> = <c>false</c>
        /// </summary>
        InvalidLog
    }
    
    /// <summary>
    /// Factory for creating elements used for print mode formats.
    /// </summary>
    public interface IPrinterElementFactory
    {
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
