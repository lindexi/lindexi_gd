// See https://aka.ms/new-console-template for more information

using System.Xml.Serialization;

var f1 = new F1<F2<int>>();
var xmlSerializer = new XmlSerializer(f1.GetType());
xmlSerializer.Serialize(Console.Out, f1);

Console.WriteLine("Hello, World!");

class F1<T>
{
}

class F2<T>
{
}