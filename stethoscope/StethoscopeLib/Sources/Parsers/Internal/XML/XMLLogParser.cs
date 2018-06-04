using Stethoscope.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Stethoscope.Parsers.Internal.XML
{
    public class XMLLogParser : ILogParser
    {
        private struct TransientParserConfigs
        {
            public LogParserFailureHandling FailureHandling { get; set; }
            public Dictionary<LogAttribute, object> AdditionalAttributes;
        }

        private ILogRegistry registry;
        
        private bool validConfigs;
        private ParserPathElement[] timestampPath;
        private ParserPathElement[] messagePath;
        private Dictionary<LogAttribute, ParserPathElement[]> attributePaths;

        private TransientParserConfigs defaultTransientConfig;

        #region GetElementDataFromPath

        private static string GetElementDataFromPath(ParserPathElement[] path, XElement element)
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
            else if (path[0].Type == ParserPathElementType.DirectNamedField)
            {
                // Use an attribute
                return element.Attribute(path[0].StringValue)?.Value;
            }
            var currentNodes = new List<XNode>
            {
                element
            };
            List<XNode> buffer;
            foreach (var section in path)
            {
                switch (section.Type)
                {
                    case ParserPathElementType.IndexField:
                        buffer = new List<XNode>();
                        foreach (var node in currentNodes)
                        {
                            if (node is XContainer)
                            {
                                var newNode = (node as XContainer).Nodes().ElementAtOrDefault(section.IndexValue);
                                if (newNode != null)
                                {
                                    buffer.Add(newNode);
                                }
                            }
                        }
                        currentNodes = buffer;
                        break;
                    case ParserPathElementType.FilterField:
                        var nodeType = XmlNodeType.None;
                        switch (section.StringValue)
                        {
                            case "cdata":
                                nodeType = XmlNodeType.CDATA;
                                break;
                            case "text":
                                nodeType = XmlNodeType.Text;
                                break;
                            case "element":
                            case "elem":
                                nodeType = XmlNodeType.Element;
                                break;
                        }

                        buffer = new List<XNode>();
                        foreach (var node in currentNodes)
                        {
                            var children = node is XContainer ? (node as XContainer).Nodes() : Enumerable.Empty<XNode>();
                            var testSelf = !children.Any();
                            if (testSelf)
                            {
                                if (node.NodeType == nodeType)
                                {
                                    buffer.Add(node);
                                }
                            }
                            else
                            {
                                foreach (var child in children)
                                {
                                    if (child.NodeType == nodeType)
                                    {
                                        buffer.Add(child);
                                    }
                                }
                            }
                        }
                        currentNodes = buffer;
                        break;
                    case ParserPathElementType.NamedField:
                        buffer = new List<XNode>();
                        var isLastNode = section.FieldType != ParserPathElementFieldType.NotAValue && section.FieldType != ParserPathElementFieldType.Unknown;
                        foreach (var node in currentNodes)
                        {
                            var bufferCount = buffer.Count;
                            if (node is XContainer)
                            {
                                buffer.AddRange((node as XContainer).Nodes().Where(child => child is XElement && (child as XElement).Name.LocalName == section.StringValue));
                            }
                            if (isLastNode && node is XElement && bufferCount == buffer.Count)
                            {
                                return (node as XElement).Attribute(section.StringValue)?.Value;
                            }
                        }
                        currentNodes = buffer;
                        break;
                }
                if (currentNodes.Count == 0)
                {
                    break;
                }
            }
            if (currentNodes.Count > 0)
            {
                var topNode = currentNodes[0];
                if (topNode is XElement)
                {
                    return (topNode as XElement).Value;
                }
                else if (topNode is XCData)
                {
                    return (topNode as XCData).Value;
                }
                else if (topNode is XText)
                {
                    return (topNode as XText).Value;
                }
            }
            return null;
        }

        #endregion

        #region ProcessElement

        private static LogParserErrors ProcessCommonLogAttributes(XMLLogParser parser, ref TransientParserConfigs config, ILogEntry entry, XElement element)
        {
            foreach (var kv in parser.attributePaths)
            {
                var rawValue = GetElementDataFromPath(kv.Value, element);
                var fieldType = ParserPathElementFieldType.String;
                if (kv.Value.Length != 0)
                {
                    fieldType = kv.Value.Last().FieldType;
                }
                var value = ParserUtil.CastField(rawValue, fieldType);
                if (value != null)
                {
                    //XXX should probably have some test for checking if the value couldn't be added
                    parser.registry.AddValueToLog(entry, kv.Key, value);
                }
            }
            if (config.AdditionalAttributes != null)
            {
                foreach (var kv in config.AdditionalAttributes)
                {
                    parser.registry.AddValueToLog(entry, kv.Key, kv.Value);
                }
            }
            return LogParserErrors.OK;
        }

        private static LogParserErrors ProcessValidElement(XMLLogParser parser, ref TransientParserConfigs config, XElement element)
        {
            if (!parser.validConfigs)
            {
                return LogParserErrors.ConfigNotInitialized;
            }
            if (parser.registry == null)
            {
                return LogParserErrors.RegistryNotSet;
            }
            if (parser.timestampPath == null || parser.messagePath == null)
            {
                return LogParserErrors.ConfigValueInvalid;
            }

            var timestamp = GetElementDataFromPath(parser.timestampPath, element);
            if (string.IsNullOrWhiteSpace(timestamp))
            {
                return LogParserErrors.MissingTimestamp;
            }

            var message = GetElementDataFromPath(parser.messagePath, element);
            if (message == null)
            {
                return LogParserErrors.MissingMessage;
            }

            var entry = parser.registry.AddLog(timestamp, message);
            return ProcessCommonLogAttributes(parser, ref config, entry, element);
        }

        private static LogParserErrors ProcessInvalidElement(XMLLogParser parser, ref TransientParserConfigs config, XElement element)
        {
            var entry = parser.registry.AddFailedLog();

            var timestamp = GetElementDataFromPath(parser.timestampPath, element);
            if (!string.IsNullOrWhiteSpace(timestamp) && DateTime.TryParse(timestamp, out DateTime time))
            {
                parser.registry.AddValueToLog(entry, LogAttribute.Timestamp, time);
            }

            var message = GetElementDataFromPath(parser.messagePath, element);
            if (message != null)
            {
                parser.registry.AddValueToLog(entry, LogAttribute.Message, message);
            }

            var result = ProcessCommonLogAttributes(parser, ref config, entry, element);

            parser.registry.NotifyFailedLogParsed(entry);

            return result;
        }

        private static LogParserErrors ProcessElement(XMLLogParser parser, ref TransientParserConfigs config, XElement element)
        {
            var result = ProcessValidElement(parser, ref config, element);
            if (result != LogParserErrors.OK && !ParserUtil.IsFatal(result))
            {
                if (config.FailureHandling == LogParserFailureHandling.SkipEntries)
                {
                    return LogParserErrors.OK;
                }
                else if (config.FailureHandling == LogParserFailureHandling.MarkEntriesAsFailed)
                {
                    return ProcessInvalidElement(parser, ref config, element);
                }
            }
            return result;
        }

        #endregion

        #region Parse Functions

        private static void ParseLoop(XMLLogParser parser, ref TransientParserConfigs config, Stream input)
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
                                var finishedElement = element;
                                element = element.Parent;
                                if (element != null && element.Name == "root")
                                {
                                    if (ProcessElement(parser, ref config, finishedElement) != LogParserErrors.OK)
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
                        case XmlNodeType.Text:
                            element.Add(new XText(xmlReader.Value));
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

        private static void InternalParse(XMLLogParser parser, ref TransientParserConfigs config, Stream logStream)
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
                    ParseLoop(parser, ref config, ms);
                }
                catch
                {
                    //XXX probably want to do something here...
                }
            }
        }

        public void Parse(Stream logStream)
        {
            XMLLogParser.InternalParse(this, ref defaultTransientConfig, logStream);
        }

        #endregion

        public void SetRegistry(ILogRegistry registry)
        {
            if (this.registry != null)
            {
                throw new InvalidOperationException("Can only set registry once");
            }
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
            if (validConfigs && attributePaths != null)
            {
                throw new InvalidOperationException("Can only set config once");
            }

            validConfigs = config.IsValid;

            timestampPath = ParserUtil.ParsePath(config.TimestampPath);
            messagePath = ParserUtil.ParsePath(config.LogMessagePath);

            defaultTransientConfig = new TransientParserConfigs()
            {
                FailureHandling = config.ParsingFailureHandling
            };

            attributePaths = new Dictionary<LogAttribute, ParserPathElement[]>();

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

        #region ApplyContextConfig

        #region XMLContextParser

        private sealed class XMLContextParser : ILogParser
        {
            private XMLLogParser parser;
            private TransientParserConfigs config;

            public XMLContextParser(XMLLogParser parser, TransientParserConfigs priorConfig)
            {
                this.parser = parser;
                this.config = priorConfig;
                this.IsUsable = true;
            }

            public bool IsUsable { get; set; }

            private void CheckUsability()
            {
                if (!IsUsable)
                {
                    throw new InvalidOperationException("Parser is no longer usable outside of context");
                }
            }

            public void ApplyConfig(IDictionary<ContextConfigs, object> config)
            {
                CheckUsability();
                if (config == null || config.Count == 0)
                {
                    return;
                }

                if (config.ContainsKey(ContextConfigs.LogSource))
                {
                    var value = config[ContextConfigs.LogSource];
                    if (!(value is string))
                    {
                        throw new ArgumentException("LogSource must be a string", "config");
                    }
                    if (this.config.AdditionalAttributes == null)
                    {
                        this.config.AdditionalAttributes = new Dictionary<LogAttribute, object>();
                    }
                    else
                    {
                        // Copy so any other instances aren't affected by changes (could be even more specific and only copy if the LogSource is different from existing one, in which case make a helper function...)
                        this.config.AdditionalAttributes = new Dictionary<LogAttribute, object>(this.config.AdditionalAttributes);
                    }
                    if (this.config.AdditionalAttributes.ContainsKey(LogAttribute.LogSource))
                    {
                        this.config.AdditionalAttributes[LogAttribute.LogSource] = (string)value;
                    }
                    else
                    {
                        this.config.AdditionalAttributes.Add(LogAttribute.LogSource, (string)value);
                    }
                }
                if (config.ContainsKey(ContextConfigs.FailureHandling))
                {
                    var value = config[ContextConfigs.FailureHandling];
                    if (!(value is LogParserFailureHandling))
                    {
                        throw new ArgumentException("FailureHandling must be a LogParserFailureHandling enum", "config");
                    }
                    this.config.FailureHandling = (LogParserFailureHandling)value;
                }
            }

            public void ApplyContextConfig(IDictionary<ContextConfigs, object> config, Action<ILogParser> context)
            {
                CheckUsability();
                XMLLogParser.InternalApplyContextConfig(parser, this.config, config, context);
            }

            public void Parse(Stream logStream)
            {
                CheckUsability();
                XMLLogParser.InternalParse(parser, ref config, logStream);
            }
        }

        #endregion

        private static void InternalApplyContextConfig(XMLLogParser parser, TransientParserConfigs priorConfig, IDictionary<ContextConfigs, object> config, Action<ILogParser> context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var contextParser = new XMLContextParser(parser, priorConfig);
            contextParser.ApplyConfig(config);

            try
            {
                context(contextParser);
            }
            finally
            {
                contextParser.IsUsable = false;
            }
        }

        public void ApplyContextConfig(IDictionary<ContextConfigs, object> config, Action<ILogParser> context)
        {
            InternalApplyContextConfig(this, defaultTransientConfig, config, context);
        }

        #endregion

        //TODO: add some way to get any errors that the parser had when parsing (that isn't obvious)
    }
}
