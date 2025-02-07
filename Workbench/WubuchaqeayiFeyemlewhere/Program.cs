// See https://aka.ms/new-console-template for more information

using System.Net.ServerSentEvents;
using System.Text.Json;
using System.Text.Json.Serialization;

List<Foo> fooList =
[
    new Foo(1, "One"),
    new Foo(2, "Two"),
    new Foo(3, "Three")
];

var memoryStream = new MemoryStream();

foreach (var foo in fooList)
{
    memoryStream.Write("message: "u8);
    JsonSerializer.Serialize(memoryStream, foo);
    memoryStream.Write("\r\n"u8);
}

memoryStream.Position = 0;
foreach (var sseItem in SseParser.Create(memoryStream).Enumerate())
{
    var sseItemData = sseItem.Data;
}

Console.WriteLine("Hello, World!");


record Foo(int Num, string Name);