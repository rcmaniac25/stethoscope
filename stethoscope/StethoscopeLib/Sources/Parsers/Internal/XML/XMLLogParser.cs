using LogTracker.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LogTracker.Parsers.Internal.XML
{
    public class XMLLogParser : ILogParser
    {
        private ILogRegistry registry;

        private bool validConfigs;
        private ParserPathElement[] timestampPath;
        private ParserPathElement[] messagePath;
        private Dictionary<LogAttribute, ParserPathElement[]> attributePaths = new Dictionary<LogAttribute, ParserPathElement[]>();

        private string GetElementDataFromPath(ParserPathElement[] path, XElement element)
        {
            if (path == null)
            {
                return null;
            }
            else if (path.Length == 0)
            {
                // Just using the existing node's data
                return element.Value;
            }
            else if (path[0].Type == ParserPathElementType.NamedField)
            {
                // Use an attribute
                return element.Attribute(path[0].StringValue)?.Value;
            }
            XNode curItem = element;
            foreach (var section in path)
            {
                var breakOut = false;
                switch (section.Type)
                {
                    case ParserPathElementType.IndexField:
                        if (curItem is XContainer)
                        {
                            curItem = (curItem as XContainer).Nodes().ElementAt(section.IndexValue);
                        }
                        else
                        {
                            curItem = null;
                            breakOut = true;
                        }
                        break;
                    case ParserPathElementType.FilterField:
                        switch (section.StringValue)
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
                if (breakOut || curItem == null)
                {
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
            if (timestampPath == null || messagePath == null)
            {
                return LogParserErrors.ConfigValueInvalid;
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
                var rawValue = GetElementDataFromPath(kv.Value, element);
                var value = ParserUtil.CastField(rawValue, kv.Value.Last().FieldType);
                if (value != null)
                {
                    registry.AddValueToLog(entry, kv.Key, value);
                }
            }

            return LogParserErrors.OK;
        }

        private void ParseLoop(Stream input)
        {
            using (var xmlReader = XmlReader.Create(input))
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

        public void Parse(Stream logStream)
        {
            using (var ms = new MemoryStream())
            {
                var rootElementStringBytes = Encoding.UTF8.GetBytes("<root>");
                ms.Write(rootElementStringBytes, 0, rootElementStringBytes.Length);

                //TODO: this doesn't work for streaming, but read/write streams don't really work/exist right now
                logStream.CopyTo(ms);

                //XXX: not going to show up in streaming log
                var rootEndElementStringBytes = Encoding.UTF8.GetBytes("</root>");
                ms.Write(rootEndElementStringBytes, 0, rootEndElementStringBytes.Length);

                ms.Position = 0L;
                try
                {
                    ParseLoop(ms);
                }
                catch
                {
                    //XXX probably want to do something here...
                }
            }
        }

        public void SetRegistry(ILogRegistry registry)
        {
            this.registry = registry;
        }

        private void AddAttributePath(LogAttribute attribute, string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                var parsedPath = ParserUtil.ParsePath(path);
                if (parsedPath != null)
                {
                    attributePaths.Add(attribute, parsedPath);
                }
            }
        }

        public void SetConfig(LogConfig config)
        {
            validConfigs = config.IsValid;

            timestampPath = ParserUtil.ParsePath(config.TimestampPath);
            messagePath = ParserUtil.ParsePath(config.LogMessagePath);

            attributePaths.Clear();

#if NETSTANDARD2_0
            var logConfigType = typeof(LogConfig);
            foreach (var attribute in LogConfig.GetAttributePaths())
            {
                if (attribute.Key == LogAttribute.Timestamp || attribute.Key == LogAttribute.Message)
                {
                    continue;
                }
                AddAttributePath(attribute.Key, logConfigType.GetProperty(attribute.Value).GetValue(config) as string);
            }
#else
            AddAttributePath(LogAttribute.ThreadID, config.ThreadIDPath);
            AddAttributePath(LogAttribute.SourceFile, config.SourceFilePath);
            AddAttributePath(LogAttribute.Function, config.FunctionPath);
            AddAttributePath(LogAttribute.SourceLine, config.LogLinePath);
            AddAttributePath(LogAttribute.Level, config.LogLevelPath);
            AddAttributePath(LogAttribute.SequenceNumber, config.LogSequencePath);
            AddAttributePath(LogAttribute.Module, config.ModulePath);
            AddAttributePath(LogAttribute.Type, config.LogTypePath);
            AddAttributePath(LogAttribute.Section, config.SectionPath);
            AddAttributePath(LogAttribute.TraceID, config.TraceIdPath);
            AddAttributePath(LogAttribute.Context, config.ContextPath);
#endif
        }

        //TODO: add some way to get any errors that the parser had when parsing (that isn't obvious)
    }
}
