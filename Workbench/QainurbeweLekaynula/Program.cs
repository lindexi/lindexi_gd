// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

string text = "\x0001正常内容123🌟a_b_c，_x0001_,__";

string encodeName = XmlConvert.EncodeName(text);

var element = new XElement(encodeName)
{
};
var xmlDocument = new XmlDocument();
var xmlElement = xmlDocument.CreateElement("Root");
xmlElement.InnerText = text;
var escapeText = xmlElement.InnerXml;

element.Value = escapeText;

var stringWriter = new StringWriter();
using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings()
{

}))
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
    var decodeName = XmlConvert.DecodeName(value);

    Debug.Assert(text == decodeName);
}

Console.WriteLine("Hello, World!");
