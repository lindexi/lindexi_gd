using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace LapouRairpaltearwou
{
    class Program
    {
        static void Main(string[] args)
        {
            var foo = new Foo()
            {
                Name = "lindexi",
                Blog = "https://blog.csdn.net/lindexi_gd",
            };

            var xmlSerializer = new XmlSerializer(typeof(Foo));
            var str = new StringBuilder();

            xmlSerializer.Serialize(new StringWriter(str), foo);

            var xml = str.ToString();
            Console.WriteLine(xml);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            string text = JsonConvert.SerializeXmlNode(doc);
            Console.WriteLine("转换json");
            Console.WriteLine(text);

            doc = (XmlDocument) JsonConvert.DeserializeXmlNode(text);
            Console.WriteLine("json转xml");
            Console.WriteLine(doc.InnerXml);

            Console.Read();
        }
    }

    public class Foo
    {
        public string Name { get; set; }

        public string Blog { get; set; }
    }
}