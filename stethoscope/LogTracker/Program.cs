using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LogTracker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Need a XML document to load of log info");
                return;
            }

            var parser = new LogParser();

            using (var ms = new MemoryStream())
            {
                var rootElementStringBytes = Encoding.UTF8.GetBytes("<root>");
                ms.Write(rootElementStringBytes, 0, rootElementStringBytes.Length);

                //TODO: this doesn't work for streaming, but read/write streams don't really work/exist right now
                using (var fr = new FileStream(args[0], FileMode.Open))
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
            
            parser.PrintTrace();
        }
    }
}
