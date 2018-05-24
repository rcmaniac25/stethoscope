﻿using System;
using System.Collections.Generic;
using System.IO;

namespace LogTracker.Common
{
    public interface ILogParser
    {
        void Parse(Stream logStream);

        void ApplyContextConfig(IDictionary<ContextConfigs, object> config, Action<ILogParser> context);
    }
}
