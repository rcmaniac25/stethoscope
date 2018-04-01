using LogTracker.Log;
using LogTracker.Parsers;
using LogTracker.Printers;

using Mono.Options;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;

namespace LogTracker
{
    public class Program
    {
        private LogParser parser;

        public Program(LogConfig config)
        {
            var parserFactory = LogParserFactory.GetParserForFileExtension("xml");
            var printerFactory = PrinterFactory.CrateConsoleFactory();

            parser = new LogParser(config, parserFactory, printerFactory);
        }
        
        public void Process(string logFile)
        {
            parser.Process(logFile);
        }

        public void Print()
        {
            parser.PrintTrace();
        }

        public static void Main(string[] args)
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
                return;
            }

            if (extraArgs.Count == 0)
            {
                Console.Error.WriteLine("Usage: LogTracker <xml log file> [<xml log config json>]");
                return;
            }

            var logConfig = new LogConfig();
            if (!string.IsNullOrWhiteSpace(logConfigPath))
            {
                using (var fs = new FileStream(logConfigPath, FileMode.Open))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        using (var jr = new JsonTextReader(sr))
                        {
                            var serializer = new JsonSerializer();
                            logConfig = serializer.Deserialize<LogConfig>(jr);
                        }
                    }
                }
            }

            var program = new Program(logConfig);

            program.Process(extraArgs[0]);

            program.Print();
        }
    }
}
