// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using System.Text.Json.Serialization;

var foo = new Foo()
{
    Component =
    [
        new Component1()
        {
            F1 = "F1"
        },
        new Component2()
        {
            F2 = "F2"
        }
    ]
};

var json = JsonSerializer.Serialize(foo, AppJsonSerializerContext.Default.Options);

// System.NotSupportedException:“Deserialization of interface or abstract types is not supported. Type 'IComponent'. Path: $.Component[0] | LineNumber: 2 | BytePositionInLine: 5.”
var foo2 = JsonSerializer.Deserialize<Foo>(json, AppJsonSerializerContext.Default.Options);

Console.WriteLine("Hello, World!");


[JsonDerivedType(typeof(Component1))]
[JsonDerivedType(typeof(Component2))]
interface IComponent
{
}

class Component1 : IComponent
{
    public string? F1 { get; set; }
}

class Component2 : IComponent
{
    public string? F2 { get; set; }
}

class Foo
{
    public List<IComponent>? Component { get; set; }
}

[JsonSerializable(typeof(IComponent))]
[JsonSerializable(typeof(Foo))]
[JsonSerializable(typeof(Component1))]
[JsonSerializable(typeof(Component2))]
[JsonSourceGenerationOptions(WriteIndented = true)]
partial class AppJsonSerializerContext : JsonSerializerContext
{
}