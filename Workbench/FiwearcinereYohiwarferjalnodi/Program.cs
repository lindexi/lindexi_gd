// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using FiwearcinereYohiwarferjalnodi;

var foo = new Foo()
{
    Component =
        new Dictionary<string, IComponent>()
        {
            {
                "1", new Component1()
                {
                    F1 = "F1"
                }
            },
            {
                "0", new Component2()
                {
                    F2 = "F2"
                }
            }
        }
};

/*
{
     "Component": {
       "1": {
         "$type": "c1",
         "F1": "F1"
       },
       "0": {
         "$type": "c2",
         "F2": "F2"
       }
     }
   }
 */
var json = JsonSerializer.Serialize(foo, typeof(Foo), FiwearcinereYohiwarferjalnodi.AppJsonSerializerContext.Default);

// System.NotSupportedException:“Deserialization of interface or abstract types is not supported. Type 'IComponent'. Path: $.Component[0] | LineNumber: 2 | BytePositionInLine: 5.”
var foo2 = JsonSerializer.Deserialize(json, typeof(Foo),
    FiwearcinereYohiwarferjalnodi.AppJsonSerializerContext.Default);

foreach (var component in (foo2 as Foo)?.Component)
{
    Console.WriteLine($"序列化 {component.Key} = {component.Value.GetType()}");
}

Console.WriteLine("Hello, World!");