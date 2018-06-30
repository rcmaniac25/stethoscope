namespace Stethoscope.Common
{
    /// <summary>
    /// Printers print log data.
    /// </summary>
    public interface IPrinter
    {
        /// <summary>
        /// Invoke a print operation.
        /// </summary>
        void Print();

        /// <summary>
        /// Setup the printer. Call before invoking <see cref="Print"/>
        /// </summary>
        void Setup();
        /// <summary>
        /// Teardown the printer. Call once all printing is complete.
        /// </summary>
        void Teardown();
    }
}
