using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace NileeCowwho
{
    class Program
    {
        static void Main(string[] args)
        {
            var reserXelpiRoorairlurmer = new XmlSerializer(typeof(MuyorkearTisdusilu));
            var muyorkearTisdusilu = new MuyorkearTisdusilu();

            var str = new StringBuilder();
            TextWriter toucoheaSairoubeejasGoraxallza = new StringWriter(str);
            reserXelpiRoorairlurmer.Serialize(toucoheaSairoubeejasGoraxallza, muyorkearTisdusilu);

            Console.WriteLine(str.ToString());
        }
    }
}