// See https://aka.ms/new-console-template for more information

using System.Xml.Serialization;
using YurkallkowhawfearrallkuWanagercika;

var f1 = new F1<F2<int>>()
{
    Value = new F2<int>()
    {
        Value = 100
    }
};
var xmlSerializer = new XmlSerializer(f1.GetType());
xmlSerializer.Serialize(Console.Out, f1);


