// See https://aka.ms/new-console-template for more information

using System.Xml;

var text = "Hello, World!你好世界\u0001";

//for (var i = 0; i < text.Length; i++)
//{
//    var c = text[i];
//    if (XmlConvert.IsXmlChar(c))
//    {
//        Console.WriteLine($"'{c}' is a valid XML character.");
//    }
//    else
//    {
//        Console.WriteLine($"'{c}' is not a valid XML character.");
//    }
//}

Console.WriteLine(XmlConvert.EncodeName(text));
