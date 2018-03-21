using System;
using System.IO;
using System.Text;
using System.Xml;

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
                    while (xmlReader.Read())
                    {
                        //TODO
                        if (!parser.HandleNewXmlElement(null))
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
