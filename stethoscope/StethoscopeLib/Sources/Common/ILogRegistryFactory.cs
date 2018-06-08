using System;

namespace Stethoscope.Common
{
    [Flags]
    public enum RegistrySelectionCriteria
    {
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

    public interface ILogRegistryFactory
    {
        ILogRegistry Create(RegistrySelectionCriteria selectionCriteria = RegistrySelectionCriteria.Default);
    }
}
