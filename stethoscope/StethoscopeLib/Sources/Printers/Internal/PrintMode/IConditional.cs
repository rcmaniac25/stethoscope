using Stethoscope.Common;

namespace Stethoscope.Printers.Internal.PrintMode
{
    /// <summary>
    /// A conditional option for deciding if a log element gets printed or not
    /// </summary>
    public interface IConditional
    {
        /// <summary>
        /// Generate a state for use.
        /// </summary>
        /// <returns>The generated state.</returns>
        object GenerateState();

        /// <summary>
        /// Determine if a log should be processed.
        /// </summary>
        /// <param name="log">The log entry to check.</param>
        /// <param name="state">A state object for processing.</param>
        /// <returns><c>true</c> if it should be processed <c>false</c> otherwise</returns>
        bool ShouldProcess(ILogEntry log, object state);

        /// <summary>
        /// The log has been processed, update state if needed.
        /// </summary>
        /// <param name="log">The log that was processed.</param>
        /// <param name="priorState">The prior version of the state.</param>
        /// <returns>The updated state, if updated.</returns>
        object Processed(ILogEntry log, object priorState);
    }
}
