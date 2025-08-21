// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using FiwearcinereYohiwarferjalnodi;

IComponent c1 = new Component1()
{
    F1 = "A2"
};

var j1 = JsonSerializer.Serialize(c1,FiwearcinereYohiwarferjalnodi.AppJsonSerializerContext.Default.Options);

var foo = new Foo()
{
    C1 = new Component1()
    {
        F1 = "A1"
    },

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

/*
{
     "Component": [
       {
         "F1": "F1"
       },
       {
         "F2": "F2"
       }
     ]
   }
 */
var json = JsonSerializer.Serialize(foo, typeof(Foo), FiwearcinereYohiwarferjalnodi.AppJsonSerializerContext.Default);

// System.NotSupportedException:“Deserialization of interface or abstract types is not supported. Type 'IComponent'. Path: $.Component[0] | LineNumber: 2 | BytePositionInLine: 5.”
var foo2 = JsonSerializer.Deserialize(json, typeof(Foo), FiwearcinereYohiwarferjalnodi.AppJsonSerializerContext.Default);

foreach (var component in (foo2 as Foo)?.Component ?? [])
{
    Console.WriteLine($"序列化 {component.GetType()}");
}

Console.WriteLine("Hello, World!");

