using Stethoscope.Common;

using System.Threading;
using System.Threading.Tasks;

namespace Stethoscope.Printers.Internal
{
    //TODO: C# 8 default impl. for interfaces will be used, so this can be removed

    /// <summary>
    /// Provide defaults for IPrinter.
    /// Temporary helper class until C# 8 default impl. is available
    /// </summary>
    public abstract class BaseIPrinter : IPrinter
    {
        /// <summary>
        /// Get the default element factory (by returning <c>null</c>.
        /// </summary>
        public IPrinterElementFactory ElementFactory => null;

        /// <summary>
        /// Invoke a print operation (sync)
        /// </summary>
        public virtual void Print()
        {
            PrintAsync().Wait();
        }

        /// <summary>
        /// Invoke a print operation (async)
        /// </summary>
        /// <returns>Task representing the print operation.</returns>
        public virtual Task PrintAsync()
        {
            return PrintAsync(new CancellationToken());
        }

        /// <summary>
        /// Invoke a print operation (async)
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        /// <returns>Task representing the print operation.</returns>
        public abstract Task PrintAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Setup the printer.
        /// </summary>
        public abstract void Setup();
        /// <summary>
        /// Teardown the printer.
        /// </summary>
        public abstract void Teardown();
    }
}
