// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

Span<string> textList =
[
    "__",
    "🌟",
    "\x0001正常内容123🌟a_b_c，_x0001_,__",
];

foreach (var text in textList)
{
    var allIsXmlChar = text.All(XmlConvert.IsXmlChar);

    string encodeName = XmlConvert.EncodeName(text);

    var element = new XElement(encodeName)
    {
        Value = text,
    };

    var stringWriter = new StringWriter();
    using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings()
           {

           }))
    {
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
}

Console.WriteLine("Hello, World!");
