namespace Stethoscope.Common
{
    /// <summary>
    /// Factory interface for picking a log printer.
    /// </summary>
    public interface IPrinterFactory
    {
        /// <summary>
        /// Create a log printer.
        /// </summary>
        /// <param name="registry">The log registry in which logs will be retrieved to be printed.</param>
        /// <param name="config">Config to define printer properties.</param>
        /// <returns>Create log printer.</returns>
        IPrinter Create(ILogRegistry registry, LogConfig config);
    }
}
