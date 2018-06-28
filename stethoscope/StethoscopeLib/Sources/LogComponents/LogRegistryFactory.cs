using Metrics;

using Stethoscope.Common;
using Stethoscope.Log.Internal;
using Stethoscope.Log.Internal.Storage;

namespace Stethoscope.Log
{
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

        public static ILogRegistryFactory Create()
        {
            factoryCreationCounter.Increment();

            return new LogRegistryFactoryFinder();
        }

        private class LogRegistryFactoryFinder : ILogRegistryFactory
        {
            public ILogRegistry Create(RegistrySelectionCriteria criteria)
            {
                creationCounter.Increment(criteria.ToString());
                
                IRegistryStorage storage;
                if (criteria == RegistrySelectionCriteria.Null)
                {
                    storage = new NullStorage(LogAttribute.Timestamp);
                }
                else
                {
                    //XXX we don't care about criteria for now, but it will be used to pick "storage"
                    storage = new ListStorage();
                }
                
                return new LogRegistry(storage);
            }
        }
    }
}
