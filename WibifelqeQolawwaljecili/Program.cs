using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace WibifelqeQolawwaljecili
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = new FileInfo("slide1.xml");
            using var fileStream = file.OpenRead();

            var xDocument = XDocument.Load(fileStream);
            foreach (var xElement in xDocument.Elements())
            {
                if (xElement is IXmlLineInfo xmlLineInfo)
                {
                    var lineNumber = xmlLineInfo.LineNumber;
                }
            }
        }
    }
}
