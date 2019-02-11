using System.Collections.Generic;

namespace Stethoscope.Printers.Internal.PrintMode
{
    /// <summary>
    /// Modify the behavior of attribute printing
    /// </summary>
    public interface IModifier
    {
        /// <summary>
        /// Setup a state for applying the modifier. This will generate values that are needed in order to apply the modifier.
        /// </summary>
        /// <param name="message">The message that will be written.</param>
        /// <returns>The generated state.</returns>
        IDictionary<string, object> GenerateInitialState(string message);

        /// <summary>
        /// Apply the modifier to the generated state.
        /// </summary>
        /// <param name="state">The state to modify by applying state.</param>
        /// <returns>The modified state.</returns>
        IDictionary<string, object> Apply(IDictionary<string, object> state);

        /// <summary>
        /// Convert from the generated state to a final string that cna be used elsewhere.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>The final string.</returns>
        string ExportFinalString(IDictionary<string, object> state);
    }
}
