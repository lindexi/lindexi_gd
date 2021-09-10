using System;
using System.IO;
using System.Xml;

namespace WibifelqeQolawwaljecili
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = new FileInfo("slide1.xml");
            using var fileStream = file.OpenRead();
            var xmlReader = XmlReader.Create(fileStream);
            var readElementContentAsString = xmlReader.ReadElementContentAsString();
        }
    }
}
