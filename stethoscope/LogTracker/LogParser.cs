﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace LogTracker
{
    public class LogParser
    {
        private ILogParser<XElement> logParser;
        private LogRegistry registry;

        public LogParser(LogConfig config)
        {
            registry = new LogRegistry();

            logParser = new XMLLogParser();
            logParser.SetConfig(config);
            logParser.SetRegistry(registry);
        }

        public bool HandleXmlElement(XElement element)
        {
            var error = logParser.ProcessLog(element);
            return error == LogParserErrors.OK;
        }
        
        private static string GenerateIndentLog(int indent)
        {
            return new string(' ', indent * 2); //XXX config for indent size
        }

        public void PrintTrace()
        {
            var threads = registry.GetBy(LogAttribute.ThreadID);
            foreach (var thread in threads)
            {
                int indent = 0;
                Console.WriteLine($"Thread {thread.Key}");

                var initSrc = thread.Value[0].GetAttribute<string>(LogAttribute.SourceFile);
                var lastFunc = thread.Value[0].GetAttribute<string>(LogAttribute.Function);

                var lastFuncSrc = $"{initSrc}.{lastFunc}";

                Console.WriteLine($"Start {lastFunc} // {initSrc}");
                foreach (var log in thread.Value)
                {
                    var src = log.GetAttribute<string>(LogAttribute.SourceFile);
                    var function = log.GetAttribute<string>(LogAttribute.Function);
                    var funcSrc = $"{src}.{function}";

                    if (funcSrc != lastFuncSrc)
                    {
                        Console.WriteLine($"End {lastFunc}");
                        Console.WriteLine($"Start {function} // {src}");

                        lastFuncSrc = funcSrc;
                        lastFunc = function;
                    }

                    Console.WriteLine($"{GenerateIndentLog(indent + 1)}{log.Message}");
                }

                Console.WriteLine($"End {lastFunc}");
                Console.WriteLine();
            }
        }
    }
}
