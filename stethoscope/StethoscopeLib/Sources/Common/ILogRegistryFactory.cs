using System;

namespace Stethoscope.Common
{
    /// <summary>
    /// Selection criteria for picking registry storage for a log registry.
    /// </summary>
    [Flags]
    public enum RegistrySelectionCriteria
    {
        /// <summary>
        /// A No-Op log storage option. Any implemented functionality is a convenience. No data is stored.
        /// </summary>
        Null = 0,

        /// <summary>
        /// Default log for many uses
        /// </summary>
        Default = ExpectSmallDataset | MemoryStorage,

        /// <summary>
        /// Log dataset is expected to be no more then a few million items.
        /// </summary>
        ExpectSmallDataset = 0x1,
        /// <summary>
        /// Log dataset is expected to be larger then a few million items.
        /// </summary>
        ExpectLargeDataset = 0x2,

        /// <summary>
        /// Log data can be stored in memory. Faster, but upon crash it can't be recovered.
        /// </summary>
        MemoryStorage = 0x4,
        /// <summary>
        /// Log data is stored in a file. Slower, but upon crash it can be recovered.
        /// </summary>
        FileStorage = 0x8
    }

    /// <summary>
    /// Factory interface to create a log registry.
    /// </summary>
    public interface ILogRegistryFactory
    {
        /// <summary>
        /// Create a log registry.
        /// </summary>
        /// <param name="selectionCriteria">Criteria for picking the storage system for the registry.</param>
        /// <returns>Created log registry.</returns>
        ILogRegistry Create(RegistrySelectionCriteria selectionCriteria = RegistrySelectionCriteria.Default);
    }
}
