using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace LogTracker
{
    public class FunctionLog
    {
        public string Name { get; private set; }
        public IList<string> Logs { get; private set; }

        public FunctionLog(string name)
        {
            Name = name;
            Logs = new List<string>();
        }
    }

    public class FileLog
    {
        public string Path { get; private set; }
        public IList<FunctionLog> Functions { get; private set; }

        private string lastFunction;

        public FileLog(string path)
        {
            Path = path;
            Functions = new List<FunctionLog>();
        }

        public void AddLog(string function, string log)
        {
            if (lastFunction != function)
            {
                Functions.Add(new FunctionLog(function));
                lastFunction = function;
            }
            Functions[Functions.Count - 1].Logs.Add(log);
        }
    }

    public class ThreadLog
    {
        public string ThreadID { get; private set; }
        public IList<FileLog> Files { get; private set; }

        private string lastFile;
        
        public ThreadLog(string id)
        {
            ThreadID = id;
            Files = new List<FileLog>();
        }

        public void AddLog(string file, string function, string log)
        {
            if (lastFile != file)
            {
                Files.Add(new FileLog(file));
                lastFile = file;
            }
            Files[Files.Count - 1].AddLog(function, log);
        }
    }

    public class LogParser
    {
        private Dictionary<string, ThreadLog> logThreads = new Dictionary<string, ThreadLog>();

        private LogConfig config;

        public LogParser(LogConfig config)
        {
            this.config = config;
        }

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
                else if(section[0] == '$')
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
            return "<could not parse path>";
        }

        public bool HandleXmlElement(XElement element)
        {
            if (!config.IsValid)
            {
                //TODO: try and guess the parameters from a log line
            }
            if (!config.IsValid)
            {
                return false;
            }
            
            //XXX while logs shouldn't be out of order, it's possible

            var threadId = element.Attribute(config.ThreadIDAttributeName).Value;
            if (!logThreads.ContainsKey(threadId))
            {
                logThreads.Add(threadId, new ThreadLog(threadId));
            }

            var thread = logThreads[threadId];
            thread.AddLog(element.Attribute(config.SourceFileAttributeName).Value, element.Attribute(config.FunctionAttributeName).Value, GetElementDataFromPath(config.LogMessagePath, element));

            //TODO
            return true;
        }

        public void ResetLogs()
        {
            logThreads.Clear();
        }
        
        private static string GenerateIndentLog(int indent)
        {
            return new string(' ', indent * 2); //XXX config for indent size
        }

        public void PrintTrace()
        {
            foreach (var thread in logThreads.Values)
            {
                int indent = 0;
                Console.WriteLine($"Thread {thread.ThreadID}");
                foreach (var file in thread.Files)
                {
                    foreach (var function in file.Functions)
                    {
                        Console.WriteLine($"Start {function.Name} // {file.Path}");

                        indent++;
                        foreach (var log in function.Logs)
                        {
                            Console.WriteLine($"{GenerateIndentLog(indent)}{log}");
                        }
                        indent--;

                        Console.WriteLine($"End {function.Name}");
                    }
                }
                Console.WriteLine();
            }
        }
    }
}
