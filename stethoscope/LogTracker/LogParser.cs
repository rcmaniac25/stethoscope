using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace LogTracker
{
    public class LogParser
    {
        public bool HandleXmlElement(XElement element)
        {
            Console.WriteLine($"=> {element.Attribute("src").Value}:{element.Attribute("sln").Value} - {element.Attribute("fun").Value}");
            //TODO
            return true;
        }
    }
}
