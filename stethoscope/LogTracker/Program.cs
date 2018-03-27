using Mono.Options;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LogTracker
{
    public class Program
    {
        private LogParser parser;

        public Program(LogConfig config)
        {
            parser = new LogParser(config);
        }

        //XXX refactor to XMLLogParser
        public void Process(string logFile)
        {
            using (var ms = new MemoryStream())
            {
                var rootElementStringBytes = Encoding.UTF8.GetBytes("<root>");
                ms.Write(rootElementStringBytes, 0, rootElementStringBytes.Length);

                //TODO: this doesn't work for streaming, but read/write streams don't really work/exist right now
                using (var fr = new FileStream(logFile, FileMode.Open))
                {
                    fr.CopyTo(ms);
                }

                //XXX: not going to show up in streaming log
                var rootEndElementStringBytes = Encoding.UTF8.GetBytes("</root>");
                ms.Write(rootEndElementStringBytes, 0, rootEndElementStringBytes.Length);

                ms.Position = 0L;
                using (var xmlReader = XmlReader.Create(ms))
                {
                    XElement element = null;
                    bool exitLoop = false;

                    while (!exitLoop && xmlReader.Read())
                    {
                        switch (xmlReader.NodeType)
                        {
                            case XmlNodeType.Element:
                                var name = XName.Get(xmlReader.Name, xmlReader.NamespaceURI);
                                if (element == null)
                                {
                                    element = new XElement(name);
                                }
                                else
                                {
                                    var ele = new XElement(name);
                                    element.Add(ele);
                                    element = ele;
                                }
                                if (xmlReader.HasAttributes)
                                {
                                    while (xmlReader.MoveToNextAttribute())
                                    {
                                        var attName = XName.Get(xmlReader.Name, xmlReader.NamespaceURI);
                                        var att = new XAttribute(attName, xmlReader.Value);
                                        element.Add(att);
                                    }
                                    xmlReader.MoveToElement();
                                }
                                break;
                            case XmlNodeType.EndElement:
                                if (xmlReader.Name == element.Name)
                                {
                                    var ele = element;
                                    element = element.Parent;
                                    if (element != null && element.Name == "root")
                                    {
                                        if (!parser.HandleXmlElement(ele))
                                        {
                                            exitLoop = true;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    Console.Error.WriteLine($"Element {element.Name} ended, but the name of the ending element {xmlReader.Name} doesn't match. Possibly out of sync...");
                                }
                                break;
                            case XmlNodeType.CDATA:
                                element.Add(new XCData(xmlReader.Value));
                                break;
                            case XmlNodeType.Whitespace:
                                break;
                            default:
                                break;
                        }
                    }

                    if (element?.Parent != null)
                    {
                        Console.Error.WriteLine("Root element didn't end");
                    }
                }
            }
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
