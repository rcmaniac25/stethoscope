using Stethoscope.Common;
using Stethoscope.Log.Internal;

namespace Stethoscope.Log
{
    public class LogRegistryFactory
    {
        private LogRegistryFactory()
        {
        }

        public static ILogRegistryFactory Create() => new LogRegistryFactoryFinder();

        private class LogRegistryFactoryFinder : ILogRegistryFactory
        {
            public ILogRegistry Create(RegistrySelectionCriteria criteria)
            {
                //XXX we don't care about criteria for now, but it will be used to pick "storage"
                return new LogRegistry(null);
            }
        }
    }
}
