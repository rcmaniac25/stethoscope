using System.Threading.Tasks;

namespace Stethoscope.Common
{
    /// <summary>
    /// Printers print log data.
    /// </summary>
    public interface IPrinter
    {
        /// <summary>
        /// Invoke a print operation (sync)
        /// </summary>
        void Print();

        /// <summary>
        /// Invoke a print operation (async)
        /// </summary>
        /// <returns>Task for managing the print.</returns>
        Task PrintAsync();

        /// <summary>
        /// Setup the printer. Call before invoking <see cref="Print"/> or <see cref="PrintAsync"/>
        /// </summary>
        void Setup();
        /// <summary>
        /// Teardown the printer. Call once all printing is complete.
        /// </summary>
        void Teardown();
    }
}
