using Metrics;

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
    /// <summary>
    /// Log parser for XML logs.
    /// </summary>
    public class XMLLogParser : ILogParser
    {
        private struct TransientParserConfigs
        {
            public LogParserFailureHandling FailureHandling { get; set; }
            public bool LogHasRoot { get; set; }
            public Dictionary<LogAttribute, object> AdditionalAttributes;
        }
        
        private static readonly Counter getElementPathTypeCounter;
        private static readonly Counter getElementFilterTypeCounter;
        private static readonly Counter unknownTopNodeTypeCounter;
        private static readonly Counter failureToAddAttributeToLogCounter;
        private static readonly Histogram attributesParsedHistogram;
        private static readonly Counter processElementCounter;
        private static readonly Timer processElementTimer;
        private static readonly Counter xmlElementCounter;
        private static readonly Counter xmlCDATACounter;
        private static readonly Counter xmlTextCounter;
        private static readonly Counter unknownXmlElementCounter;
        private static readonly Counter xmlRootUnfinishedCounter;
        private static readonly Counter parseCounter;
        private static readonly Counter setConfigFailureHandlingCounter;
        private static readonly Counter contextApplyConfigCounter;
        private static readonly Histogram contextApplyConfigSizeHistogram;
        private static readonly Counter contextApplyContextConfigCounter;
        private static readonly Counter contextParseCounter;
        private static readonly Counter applyContextConfigCounter;

        static XMLLogParser()
        {
            var xmlParserContext = Metric.Context("XMLLogParser");
            getElementPathTypeCounter = xmlParserContext.Counter("GetElementDataFromPath Path Type", Unit.Items, "xml, log, parser, get, attribute, path");
            getElementFilterTypeCounter = xmlParserContext.Counter("GetElementDataFromPath Filter Type", Unit.Items, "xml, log, parser, get, attribute, filter");
            unknownTopNodeTypeCounter = xmlParserContext.Counter("Unknown Top Node Type", Unit.Errors, "xml, log, parser, get, attribute, unknown");
            failureToAddAttributeToLogCounter = xmlParserContext.Counter("Failure to Add Attribute to Log", Unit.Errors, "xml, log, parser, process, element, failure, add");
            attributesParsedHistogram = xmlParserContext.Histogram("Attributes Parsed", Unit.Items, tags: "xml, log, parser, process, element, items");
            processElementCounter = xmlParserContext.Counter("ProcessElement", Unit.Calls, "xml, log, parser, process, element");
            {
                var subContext = xmlParserContext.Context("Parse");
                processElementTimer = subContext.Timer("ProcessElement", Unit.Calls, tags: "xml, log, parser, parse, process");
                xmlElementCounter = subContext.Counter("XML Element", Unit.Items, "xml, log, parser, parse, element");
                xmlCDATACounter = subContext.Counter("XML CDATA", Unit.Items, "xml, log, parser, parse, text, cdata");
                xmlTextCounter = subContext.Counter("XML Text", Unit.Items, "xml, log, parser, parse, text");
                unknownXmlElementCounter = subContext.Counter("Unknown XML Types", Unit.Items, "xml, log, parser, parse, unknown");
                parseCounter = subContext.Counter("Unknown XML Types", Unit.Items, "xml, log, parser, parse, unknown");
                xmlRootUnfinishedCounter = subContext.Counter("XML root Unfinished", Unit.Errors, "xml, log, parser, parse, root, unfinished");
            }
            parseCounter = xmlParserContext.Counter("Parse", Unit.Calls, "xml, log, parser, parse");
            setConfigFailureHandlingCounter = xmlParserContext.Counter("SetConfig", Unit.Calls, "xml, log, parser, config, failure, handle");
            {
                var subContext = xmlParserContext.Context("Context");
                contextApplyConfigCounter = subContext.Counter("ApplyConfig", Unit.Calls, "xml, log, parser, context, config");
                contextApplyConfigSizeHistogram = subContext.Histogram("ApplyConfig Size", Unit.Items, tags: "xml, log, parser, context, config, size");
                contextApplyContextConfigCounter = subContext.Counter("ApplyContextConfig", Unit.Calls, "xml, log, parser, context, config");
                contextParseCounter = subContext.Counter("Parse", Unit.Calls, "xml, log, parser, context, parse");
            }
            applyContextConfigCounter = xmlParserContext.Counter("ApplyContextConfig", Unit.Calls, "xml, log, parser, context, config");
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
                getElementPathTypeCounter.Increment(ParserPathElementType.DirectNamedField.ToString());
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
                        getElementPathTypeCounter.Increment(ParserPathElementType.IndexField.ToString());
                        buffer = new List<XNode>();
                        foreach (var node in currentNodes)
                        {
                            if (node is XContainer container)
                            {
                                var newNode = container.Nodes().ElementAtOrDefault(section.IndexValue);
                                if (newNode != null)
                                {
                                    buffer.Add(newNode);
                                }
                            }
                        }
                        currentNodes = buffer;
                        break;
                    case ParserPathElementType.FilterField:
                        getElementPathTypeCounter.Increment(ParserPathElementType.FilterField.ToString());
                        getElementFilterTypeCounter.Increment(section.StringValue);

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
                            var children = node is XContainer container ? container.Nodes() : Enumerable.Empty<XNode>();
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
                        getElementPathTypeCounter.Increment(ParserPathElementType.NamedField.ToString());
                        buffer = new List<XNode>();
                        var isLastNode = section.FieldType != ParserPathElementFieldType.NotAValue && section.FieldType != ParserPathElementFieldType.Unknown;
                        foreach (var node in currentNodes)
                        {
                            var bufferCount = buffer.Count;
                            if (node is XContainer container)
                            {
                                buffer.AddRange(container.Nodes().Where(child => child is XElement e && e.Name.LocalName == section.StringValue));
                            }
                            if (isLastNode && node is XElement elem && bufferCount == buffer.Count)
                            {
                                return elem.Attribute(section.StringValue)?.Value;
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
                if (topNode is XElement elem)
                {
                    return elem.Value;
                }
                else if (topNode is XCData data)
                {
                    return data.Value;
                }
                else if (topNode is XText text)
                {
                    return text.Value;
                }
                else
                {
                    unknownTopNodeTypeCounter.Increment(topNode.NodeType.ToString());
                }
            }
            return null;
        }

        #endregion

        #region ProcessElement
        
        private static LogParserErrors ProcessCommonLogAttributes(XMLLogParser parser, ref TransientParserConfigs config, ILogEntry entry, XElement element)
        {
            var attributesAdded = 0;
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
                    if (parser.registry.AddValueToLog(entry, kv.Key, value))
                    {
                        attributesAdded++;
                    }
                    else
                    {
                        failureToAddAttributeToLogCounter.Increment(kv.Key.ToString());
                    }
                }
            }
            if (config.AdditionalAttributes != null)
            {
                foreach (var kv in config.AdditionalAttributes)
                {
                    if (parser.registry.AddValueToLog(entry, kv.Key, kv.Value))
                    {
                        attributesAdded++;
                    }
                    else
                    {
                        failureToAddAttributeToLogCounter.Increment(kv.Key.ToString());
                    }
                }
            }
            attributesParsedHistogram.Update(attributesAdded);
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
            processElementCounter.Increment();
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
                // Don't just use XML's natual tree-creation setup... because we don't want the root element to have children. For large or streaming logs, it will become a resource hog
                var elements = new Stack<XElement>();
                bool exitLoop = false;

                while (!exitLoop && xmlReader.Read())
                {
                    switch (xmlReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            var name = XName.Get(xmlReader.Name, xmlReader.NamespaceURI);
                            var element = new XElement(name);
                            if (elements.Count > 1) // Don't add children to root to ensure it doesn't become massive
                            {
                                elements.Peek().Add(element);
                            }
                            elements.Push(element);
                            if (xmlReader.HasAttributes)
                            {
                                while (xmlReader.MoveToNextAttribute())
                                {
                                    var attName = XName.Get(xmlReader.Name, xmlReader.NamespaceURI);
                                    var att = new XAttribute(attName, xmlReader.Value);
                                    elements.Peek().Add(att);
                                }
                                xmlReader.MoveToElement();
                            }
                            break;
                        case XmlNodeType.EndElement:
                            xmlElementCounter.Increment();
                            if (xmlReader.Name == elements.Peek().Name)
                            {
                                var finishedElement = elements.Pop();
                                if (elements.Count == 1) // Don't process the elements unless they're children of root. Anything else is a child element of a log element
                                {
                                    using (processElementTimer.NewContext())
                                    {
                                        if (ProcessElement(parser, ref config, finishedElement) != LogParserErrors.OK)
                                        {
                                            exitLoop = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.Error.WriteLine($"Element {elements.Peek().Name} ended, but the name of the ending element {xmlReader.Name} doesn't match. Possibly out of sync...");
                            }
                            break;
                        case XmlNodeType.CDATA:
                            xmlCDATACounter.Increment();
                            elements.Peek().Add(new XCData(xmlReader.Value));
                            break;
                        case XmlNodeType.Text:
                            xmlTextCounter.Increment();
                            elements.Peek().Add(new XText(xmlReader.Value));
                            break;
                        case XmlNodeType.Whitespace:
                            break;
                        default:
                            unknownXmlElementCounter.Increment(xmlReader.NodeType.ToString());
                            break;
                    }
                }

                if (elements.Count != 0)
                {
                    xmlRootUnfinishedCounter.Increment();
                    Console.Error.WriteLine("Root element didn't end");
                }
            }
        }

        private static void InternalParse(XMLLogParser parser, ref TransientParserConfigs config, Stream logStream)
        {
            using (var ms = new MemoryStream())
            {
                if (!config.LogHasRoot)
                {
                    var rootElementStringBytes = Encoding.UTF8.GetBytes("<root>");
                    ms.Write(rootElementStringBytes, 0, rootElementStringBytes.Length);
                }

                //TODO: this doesn't work for streaming, but read/write streams don't really work/exist right now
                logStream.CopyTo(ms);

                if (!config.LogHasRoot)
                {
                    //XXX: not going to show up in streaming log
                    var rootEndElementStringBytes = Encoding.UTF8.GetBytes("</root>");
                    ms.Write(rootEndElementStringBytes, 0, rootEndElementStringBytes.Length);
                }

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

        /// <summary>
        /// Parse a stream of data to get applicable logs.
        /// </summary>
        /// <param name="logStream">The stream of log data.</param>
        public void Parse(Stream logStream)
        {
            parseCounter.Increment();
            XMLLogParser.InternalParse(this, ref defaultTransientConfig, logStream);
        }

        #endregion

        /// <summary>
        /// Set the registy that logs will be saved to once parsed.
        /// </summary>
        /// <param name="registry">The registy that logs will be saved to once parsed.</param>
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

        /// <summary>
        /// Set the configurations to apply to the log parser.
        /// </summary>
        /// <param name="config">Configurations to apply to the log parser.</param>
        public void SetConfig(LogConfig config)
        {
            if (validConfigs && attributePaths != null)
            {
                throw new InvalidOperationException("Can only set config once");
            }

            validConfigs = config.IsValid;

            timestampPath = ParserUtil.ParsePath(config.TimestampPath);
            messagePath = ParserUtil.ParsePath(config.LogMessagePath);

            setConfigFailureHandlingCounter.Increment(config.ParsingFailureHandling.ToString());
            defaultTransientConfig = new TransientParserConfigs()
            {
                FailureHandling = config.ParsingFailureHandling,
                LogHasRoot = config.LogHasRoot
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
            private readonly XMLLogParser parser;
            private TransientParserConfigs config;

            public XMLContextParser(XMLLogParser parser, TransientParserConfigs priorConfig)
            {
                this.parser = parser;
                this.config = priorConfig;
            }

            public bool IsUsable { get; set; } = true;

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

                contextApplyConfigCounter.Increment();
                contextApplyConfigSizeHistogram.Update(config.Count);

                #region Parse config

                if (config.ContainsKey(ContextConfigs.LogSource))
                {
                    var value = config[ContextConfigs.LogSource];
                    if (!(value is string))
                    {
                        throw new ArgumentException("LogSource must be a string", nameof(config));
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
                    if (value is LogParserFailureHandling handling)
                    {
                        this.config.FailureHandling = handling;
                    }
                    else
                    {
                        throw new ArgumentException("FailureHandling must be a LogParserFailureHandling enum", nameof(config));
                    }
                }
                if (config.ContainsKey(ContextConfigs.LogHasRoot))
                {
                    var value = config[ContextConfigs.LogHasRoot];
                    if (value is bool hasRoot)
                    {
                        this.config.LogHasRoot = hasRoot;
                    }
                    else
                    {
                        throw new ArgumentException("LogHasRoot must be a boolean", nameof(config));
                    }
                }

                #endregion
            }
            
            public void ApplyContextConfig(IDictionary<ContextConfigs, object> config, Action<ILogParser> context)
            {
                CheckUsability();
                contextApplyContextConfigCounter.Increment();
                XMLLogParser.InternalApplyContextConfig(parser, this.config, config, context);
            }

            public void Parse(Stream logStream)
            {
                CheckUsability();
                contextParseCounter.Increment();
                XMLLogParser.InternalParse(parser, ref config, logStream);
            }
        }

        #endregion

        private static void InternalApplyContextConfig(XMLLogParser parser, TransientParserConfigs priorConfig, IDictionary<ContextConfigs, object> config, Action<ILogParser> context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
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

        /// <summary>
        /// Apply additional context to the parser, using specific configs to modify parsing.
        /// </summary>
        /// <param name="config">A collection of configs to modify the parser with.</param>
        /// <param name="context">The context that the modified parser will execute in. If the scope of this parser is exited, as in the Action delegate finishes execution, then the modified parser becomes invalid and won't run.</param>
        public void ApplyContextConfig(IDictionary<ContextConfigs, object> config, Action<ILogParser> context)
        {
            applyContextConfigCounter.Increment();
            InternalApplyContextConfig(this, defaultTransientConfig, config, context);
        }

        #endregion

        //TODO: add some way to get any errors that the parser had when parsing (that isn't obvious)
    }
}
