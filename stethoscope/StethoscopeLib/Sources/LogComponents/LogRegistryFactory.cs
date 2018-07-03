using Metrics;

using Stethoscope.Common;
using Stethoscope.Log.Internal;
using Stethoscope.Log.Internal.Storage;

namespace Stethoscope.Log
{
    /// <summary>
    /// Meta Factory object to create a log registry.
    /// </summary>
    public class LogRegistryFactory
    {
        private static readonly Counter factoryCreationCounter;
        private static readonly Counter creationCounter;

        static LogRegistryFactory()
        {
            var printerContext = Metric.Context("LogRegistry Factory");
            factoryCreationCounter = printerContext.Counter("Creation", Unit.Calls, "log, registry, factory");
            creationCounter = printerContext.Counter("Usage", Unit.Calls, "log, registry");
        }

        private LogRegistryFactory()
        {
        }

        /// <summary>
        /// Create a log registry factory.
        /// </summary>
        /// <returns>Log registry factory.</returns>
        public static ILogRegistryFactory Create()
        {
            factoryCreationCounter.Increment();

            return new LogRegistryFactoryFinder();
        }

        private class LogRegistryFactoryFinder : ILogRegistryFactory
        {
            private IRegistryStorage PickStorage(RegistrySelectionCriteria criteria)
            {
                if (criteria == RegistrySelectionCriteria.Null)
                {
                    return new NullStorage(LogAttribute.Timestamp);
                }
                else
                {
                    //XXX we don't care about criteria for now, but it will be used to pick "storage"
                    return new ListStorage();
                }
            }

            public ILogRegistry Create(RegistrySelectionCriteria criteria)
            {
                creationCounter.Increment(criteria.ToString());

                IRegistryStorage storage = PickStorage(criteria);
                return new LogRegistry(storage);
            }
        }
    }
}
