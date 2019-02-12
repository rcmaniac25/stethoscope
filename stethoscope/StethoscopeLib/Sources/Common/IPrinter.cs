using System.Threading;
using System.Threading.Tasks;

using Stethoscope.Printers.Internal;

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
        void Print(); //TODO: C# 8 default impl. for interfaces

        /// <summary>
        /// Invoke a print operation (async)
        /// </summary>
        /// <returns>Task representing the print operation.</returns>
        Task PrintAsync(); //TODO: C# 8 default impl. for interfaces

        /// <summary>
        /// Invoke a print operation (async)
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        /// <returns>Task representing the print operation.</returns>
        Task PrintAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Setup the printer. Call before invoking <see cref="Print"/> or <see cref="PrintAsync()"/>/<see cref="PrintAsync(CancellationToken)"/>
        /// </summary>
        void Setup();
        /// <summary>
        /// Teardown the printer. Call once all printing is complete.
        /// </summary>
        void Teardown();

        /// <summary>
        /// (Mostly for Internal use) Factory for specialized print mode format elements. Return <c>null</c> to use default factory.
        /// </summary>
        IPrinterElementFactory ElementFactory { get; }
    }
}
