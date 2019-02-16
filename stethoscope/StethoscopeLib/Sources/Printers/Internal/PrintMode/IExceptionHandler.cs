using Stethoscope.Common;

using System;

namespace Stethoscope.Printers.Internal.PrintMode
{
    /// <summary>
    /// When an exception occurs while printing, handle it.
    /// </summary>
    public interface IExceptionHandler : IModifier
    {
        /// <summary>
        /// Handle an exception.
        /// </summary>
        /// <param name="e">The exception that occured.</param>
        /// <param name="log">The log that was being processed.</param>
        /// <param name="element">The element that was processing the <paramref name="log"/></param>
        void HandleException(Exception e, ILogEntry log, IElement element);
    }
}
