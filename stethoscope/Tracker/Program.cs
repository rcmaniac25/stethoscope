using Stethoscope.Common;
using Stethoscope.Log;
using Stethoscope.Parsers;
using Stethoscope.Printers;

using Metrics;

using Mono.Options;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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
            catch (OptionException)
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

        public void Init()
        {
            Metric.Config.WithHttpEndpoint("http://localhost:2581/").WithSystemCounters();
            
            //XXX use config to get this info
            var registryFactory = LogRegistryFactory.Create();

            var parserFactory = LogParserFactory.GetParserForFileExtension("xml");

            IPrinterFactory printerFactory;
            if (config.UserConfigs != null && config.UserConfigs.ContainsKey("printToFile") && !string.IsNullOrWhiteSpace(config.UserConfigs["printToFile"]))
            {
                // Note: this just sets a default. Configs can change the file
                printerFactory = PrinterFactory.CrateFileFactory(config.UserConfigs["printToFile"]);
            }
            else
            {
                printerFactory = PrinterFactory.CrateConsoleFactory();
            }

            registry = registryFactory.Create();

            logFileParser = parserFactory.Create(registry, config);
            printer = printerFactory.Create(registry, config);
        }

        public async Task Process()
        {
            var parseTask = logFileParser.ParseAsync(extraParserArguments[0]);
            System.Threading.Thread.Sleep(100);
            var printTask = printer.PrintAsync();
            await printTask;
            await parseTask;
        }

        public void Start()
        {
            printer.Setup();
        }

        public void Stop()
        {
            printer.Teardown();
        }

        public static void Main(string[] args)
        {
            var program = new Program();

            if (program.ParseArguments(args))
            {
                program.Init();

                program.Start();
                program.Process().Wait();
                program.Stop();
            }
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("Press Any Key to Continue");
                Console.ReadKey();
            }
        }
    }
}
