// See https://aka.ms/new-console-template for more information

using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

var file = @"C:\lindexi\Slides\Slide_0.xml";
var xmlDocument = new XmlDocument();
xmlDocument.Load(file);

Console.WriteLine("Hello, World!");
