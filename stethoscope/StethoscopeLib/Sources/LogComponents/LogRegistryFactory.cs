using Metrics;

using Stethoscope.Common;
using Stethoscope.Log.Internal;
using Stethoscope.Log.Internal.Storage;

namespace Stethoscope.Log
{
    public class LogRegistryFactory
    {
        private static readonly Counter logRegistryFactoryCreationCounter;
        private static readonly Counter logRegistryCreationCounter;

        static LogRegistryFactory()
        {
            var printerContext = Metric.Context("LogRegistry Factory");
            logRegistryFactoryCreationCounter = printerContext.Counter("Creation", Unit.Calls, "log, registry, factory");
            logRegistryCreationCounter = printerContext.Counter("Usage", Unit.Calls, "log, registry");
        }

        private LogRegistryFactory()
        {
        }

        public static ILogRegistryFactory Create()
        {
            logRegistryFactoryCreationCounter.Increment();

            return new LogRegistryFactoryFinder();
        }

        private class LogRegistryFactoryFinder : ILogRegistryFactory
        {
            public ILogRegistry Create(RegistrySelectionCriteria criteria)
            {
                logRegistryCreationCounter.Increment();

                //TODO: record stat about criteria used
                //XXX we don't care about criteria for now, but it will be used to pick "storage"
                return new LogRegistry(new ListStorage());
            }
        }
    }
}
