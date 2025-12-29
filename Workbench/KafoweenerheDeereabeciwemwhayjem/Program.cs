// See https://aka.ms/new-console-template for more information

using System.Xml;

var testFile = @"C:\lindexi\slide1.xml";
using var fileStream = File.OpenRead(testFile);
var xmlReader = XmlReader.Create(fileStream, new XmlReaderSettings()
{
    Async = true,
});

while (!xmlReader.EOF)
{
    await xmlReader.ReadAsync();
    string name = xmlReader.Name;
    var namespaceUri = xmlReader.NamespaceURI;
    var prefix = xmlReader.Prefix;
    XmlNodeType nodeType = xmlReader.NodeType;
}

Console.WriteLine("Hello, World!");
