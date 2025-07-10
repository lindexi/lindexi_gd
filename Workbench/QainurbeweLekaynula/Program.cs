// See https://aka.ms/new-console-template for more information

using System.Xml;
using System.Xml.Linq;

string text = "\x0001";
var element = new XElement("Node")
{
    Value = text
};

var stringWriter = new StringWriter();
using (var xmlWriter = XmlWriter.Create(stringWriter))
{
    // System.ArgumentException:“'', hexadecimal value 0x01, is an invalid character.”
    var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), element);
    document.WriteTo(xmlWriter);
}
Console.WriteLine("Hello, World!");
