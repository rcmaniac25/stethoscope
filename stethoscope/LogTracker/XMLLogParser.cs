using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LogTracker
{
    public class XMLLogParser : ILogParser<XElement>
    {
        private LogRegistry registry;
        private LogConfig config;

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
            if (!config.IsValid)
            {
                return LogParserErrors.ConfigNotInitialized;
            }
            if (registry == null)
            {
                return LogParserErrors.RegistryNotSet;
            }

            //XXX while logs shouldn't be out of order, it's possible

            var timestamp = GetElementDataFromPath(config.TimestampPath, element);
            if (timestamp == null)
            {
                return LogParserErrors.MissingTimestamp;
            }

            var message = GetElementDataFromPath(config.LogMessagePath, element);
            if (message == null)
            {
                return LogParserErrors.MissingMessage;
            }

            var entry = registry.AddLog(timestamp, message);
            //TODO: parse other attributes

            //var threadId = GetElementDataFromPath(config.ThreadIDPath, element);
            //if (!logThreads.ContainsKey(threadId))
            //{
            //    logThreads.Add(threadId, new ThreadLog(threadId));
            //}

            //var thread = logThreads[threadId];
            //thread.AddLog(GetElementDataFromPath(config.SourceFilePath, element), GetElementDataFromPath(config.FunctionPath, element), GetElementDataFromPath(config.LogMessagePath, element));

            return LogParserErrors.OK;
        }

        public void SetRegistry(LogRegistry registry)
        {
            this.registry = registry;
        }

        public void SetConfig(LogConfig config)
        {
            this.config = config;
        }
    }
}
