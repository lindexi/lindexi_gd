// See https://aka.ms/new-console-template for more information

using System.Xml;
using System.Xml.Linq;

string text = "\x0001";

string encodeName = XmlConvert.EncodeName(text);

var element = new XElement("Node")
{
    Value = encodeName
};

var stringWriter = new StringWriter();
using (var xmlWriter = XmlWriter.Create(stringWriter))
{
    // System.ArgumentException:“'', hexadecimal value 0x01, is an invalid character.”
    var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), element);
    document.WriteTo(xmlWriter);
}

var xmlText = stringWriter.GetStringBuilder().ToString();

if (!string.IsNullOrEmpty(xmlText))
{
    var document = XDocument.Parse(xmlText);
    var rootElement = document.Root;
    var value = rootElement?.Value;
}

Console.WriteLine("Hello, World!");
