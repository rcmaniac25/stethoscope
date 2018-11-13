using System.IO;

namespace Stethoscope.Printers.Internal
{
    /// <summary>
    /// Log printer that prints to the standard out.
    /// </summary>
    public class FilePrinter : IOPrinter
    {
        /// <summary>
        /// Create a new file-based printer.
        /// </summary>
        /// <param name="path">Path to write to.</param>
        public FilePrinter(string path)
        {
            FilePath = path;
        }

        /// <summary>
        /// Get the file path to write to.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Setup the printer.
        /// </summary>
        public override void Setup()
        {
            TextWriter = new StreamWriter(FilePath, true); //XXX should we append, truncate, or "other"?
        }

        /// <summary>
        /// Teardown the printer.
        /// </summary>
        public override void Teardown()
        {
            TextWriter.Close();
        }

        /// <summary>
        /// Set config for a printer
        /// </summary>
        /// <param name="config">The config for the printer.</param>
        public override void SetConfig(LogConfig config)
        {
            //TODO: what if a file is specified via config to print to instead of the specified one? How is it identified?
            base.SetConfig(config);
        }
    }
}
