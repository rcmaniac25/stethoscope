﻿using Stethoscope.Common;
using Stethoscope.Log.Internal;
using Stethoscope.Log.Internal.Storage;

namespace Stethoscope.Log
{
    public class LogRegistryFactory
    {
        private LogRegistryFactory()
        {
        }

        public static ILogRegistryFactory Create() => new LogRegistryFactoryFinder(); //TODO: record stat about function used

        private class LogRegistryFactoryFinder : ILogRegistryFactory
        {
            public ILogRegistry Create(RegistrySelectionCriteria criteria)
            {
                //TODO: record stat about function and criteria used
                //XXX we don't care about criteria for now, but it will be used to pick "storage"
                return new LogRegistry(new ListStorage());
            }
        }
    }
}
