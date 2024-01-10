// See https://aka.ms/new-console-template for more information

using System.Xml.Linq;

var file = @"C:\lindexi\Slides\Slide_0.xml";

var xDocument = XDocument.Load(file);

Console.WriteLine("Hello, World!");
