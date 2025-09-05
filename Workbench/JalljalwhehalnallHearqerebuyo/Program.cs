// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using JalljalwhehalnallHearqerebuyo;

var f1 = new F1();
JsonSerializer.Serialize(f1, f1.GetType(), FooJsonSerializerContext.Default);
Console.WriteLine("Hello, World!");
