using Stethoscope.Common;
using Stethoscope.Log;
using Stethoscope.Parsers;
using Stethoscope.Printers;

using Mono.Options;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;

namespace Stethoscope
{
    public class Program
    {
        private LogConfig config;
        private string[] extraParserArguments;

        private ILogParser logFileParser;
        private IPrinter printer;

        private ILogRegistry registry;

        #region Argument Parsing

        public bool ParseArguments(string[] args)
        {
            string logConfigPath = null;

            var options = new OptionSet()
            {
                { "c|config=", v => logConfigPath = v }
            };

            var extraArgs = new List<string>();
            try
            {
                extraArgs = options.Parse(args);
            }
            catch (OptionException e)
            {
                //TODO
                return false;
            }

            if (extraArgs.Count == 0)
            {
                Console.Error.WriteLine("Usage: Tracker <xml log file> [<xml log config json>]");
                return false;
            }

            extraParserArguments = extraArgs.ToArray();

            config = new LogConfig();
            if (!string.IsNullOrWhiteSpace(logConfigPath))
            {
                using (var fs = new FileStream(logConfigPath, FileMode.Open))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        using (var jr = new JsonTextReader(sr))
                        {
                            var serializer = new JsonSerializer();
                            config = serializer.Deserialize<LogConfig>(jr);
                        }
                    }
                }
            }

            return true;
        }

        #endregion

        public void Setup()
        {
            //XXX use config to get this info
            var registryFactory = LogRegistryFactory.Create();

            var parserFactory = LogParserFactory.GetParserForFileExtension("xml");
            var printerFactory = PrinterFactory.CrateConsoleFactory();

            registry = registryFactory.Create();

            logFileParser = parserFactory.Create(registry, config);
            printer = printerFactory.Create(registry, config);
        }

        public void Process()
        {
            logFileParser.Parse(extraParserArguments[0]);
        }

        public void Print()
        {
            printer.Setup();
            printer.Print();
            printer.Teardown();
        }

        public static void Main(string[] args)
        {
            var program = new Program();

            if (program.ParseArguments(args))
            {
                program.Setup();
                program.Process();
                program.Print();
            }
        }
    }
}
