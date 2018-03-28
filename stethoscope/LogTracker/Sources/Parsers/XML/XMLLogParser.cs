using LogTracker.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LogTracker.Parsers.XML
{
    public class XMLLogParser : ILogParser
    {
        private ILogRegistry registry;

        private bool validConfigs;
        private string timestampPath;
        private string messagePath;
        private Dictionary<LogAttribute, string> attributePaths = new Dictionary<LogAttribute, string>();

        //XXX This is way overcomplicated, and yet I want to extend it further... leave it for now and if it becomes a performance bottleneck, we'll replace it
        private string GetElementDataFromPath(string path, XElement element)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "/")
            {
                // Just using the existing node's data
                return element.Value;
            }
            else if (path[0] == '!')
            {
                // Use an attribute
                return element.Attribute(path.Substring(1))?.Value;
            }
            var sections = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            XNode curItem = element;
            foreach (var section in sections)
            {
                if (section[0] == '#')
                {
                    // Index syntax
                    if (curItem is XContainer)
                    {
                        curItem = (curItem as XContainer).Nodes().ElementAt(int.Parse(section.Substring(1)));
                    }
                    else
                    {
                        curItem = null;
                        break;
                    }
                }
                else if (section[0] == '$')
                {
                    // Filter syntax
                    switch (section.Substring(1))
                    {
                        case "cdata":
                            if (curItem.NodeType != XmlNodeType.CDATA)
                            {
                                curItem = null;
                            }
                            break;
                        case "text":
                            if (curItem.NodeType != XmlNodeType.Text)
                            {
                                curItem = null;
                            }
                            break;
                    }
                    break;
                }
            }
            if (curItem != null)
            {
                if (curItem is XElement)
                {
                    return (curItem as XElement).Value;
                }
                else if (curItem is XCData)
                {
                    return (curItem as XCData).Value;
                }
                else if (curItem is XText)
                {
                    return (curItem as XText).Value;
                }
            }
            return null;
        }

        private LogParserErrors ProcessElement(XElement element)
        {
            if (!validConfigs)
            {
                return LogParserErrors.ConfigNotInitialized;
            }
            if (registry == null)
            {
                return LogParserErrors.RegistryNotSet;
            }

            //XXX while logs shouldn't be out of order, it's possible

            var timestamp = GetElementDataFromPath(timestampPath, element);
            if (string.IsNullOrWhiteSpace(timestamp))
            {
                return LogParserErrors.MissingTimestamp;
            }

            var message = GetElementDataFromPath(messagePath, element);
            if (message == null)
            {
                return LogParserErrors.MissingMessage;
            }

            var entry = registry.AddLog(timestamp, message);

            foreach (var kv in attributePaths)
            {
                //XXX Only supports strings right now
                var value = GetElementDataFromPath(kv.Value, element);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    registry.AddValueToLog(entry, kv.Key, value);
                }
            }

            return LogParserErrors.OK;
        }

        public void Parse(string logFile)
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
                                        if (ProcessElement(ele) != LogParserErrors.OK)
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

        public void SetRegistry(ILogRegistry registry)
        {
            this.registry = registry;
        }

        public void SetConfig(LogConfig config)
        {
            validConfigs = config.IsValid;

            timestampPath = config.TimestampPath;
            messagePath = config.LogMessagePath;

            attributePaths.Clear();

            //XXX too manual... how do we get this from LogConfig?
            if (!string.IsNullOrWhiteSpace(config.ThreadIDPath))
            {
                attributePaths.Add(LogAttribute.ThreadID, config.ThreadIDPath);
            }
            if (!string.IsNullOrWhiteSpace(config.SourceFilePath))
            {
                attributePaths.Add(LogAttribute.SourceFile, config.SourceFilePath);
            }
            if (!string.IsNullOrWhiteSpace(config.FunctionPath))
            {
                attributePaths.Add(LogAttribute.Function, config.FunctionPath);
            }
            if (!string.IsNullOrWhiteSpace(config.LogLinePath))
            {
                attributePaths.Add(LogAttribute.SourceLine, config.LogLinePath);
            }
            if (!string.IsNullOrWhiteSpace(config.LogLevelPath))
            {
                attributePaths.Add(LogAttribute.Level, config.LogLevelPath);
            }
            if (!string.IsNullOrWhiteSpace(config.LogSequencePath))
            {
                attributePaths.Add(LogAttribute.SequenceNumber, config.LogSequencePath);
            }
            if (!string.IsNullOrWhiteSpace(config.ModulePath))
            {
                attributePaths.Add(LogAttribute.Module, config.ModulePath);
            }
            if (!string.IsNullOrWhiteSpace(config.LogTypePath))
            {
                attributePaths.Add(LogAttribute.Type, config.LogTypePath);
            }
            if (!string.IsNullOrWhiteSpace(config.SectionPath))
            {
                attributePaths.Add(LogAttribute.Section, config.SectionPath);
            }
        }
    }
}
