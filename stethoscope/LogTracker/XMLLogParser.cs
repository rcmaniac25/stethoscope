using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace LogTracker
{
    public class XMLLogParser : ILogParser<XElement>
    {
        private LogRegistry registry;

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
                return element.Attribute(path.Substring(1)).Value;
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

        public LogParserErrors ProcessLog(XElement element)
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
            if (timestamp == null)
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
                if (value != null)
                {
                    registry.AddValueToLog(entry, kv.Key, value);
                }
            }

            return LogParserErrors.OK;
        }

        public void SetRegistry(LogRegistry registry)
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
                attributePaths.Add(LogAttribute.LogLine, config.LogLinePath);
            }
            //TODO
        }
    }
}
