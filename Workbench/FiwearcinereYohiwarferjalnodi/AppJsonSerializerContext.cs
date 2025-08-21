using System.Text.Json.Serialization;

namespace FiwearcinereYohiwarferjalnodi;

[JsonSerializable(typeof(IComponent))]
[JsonSerializable(typeof(Foo))]
[JsonSerializable(typeof(Component1))]
[JsonSerializable(typeof(Component2))]
[JsonSourceGenerationOptions(WriteIndented = true)]
partial class AppJsonSerializerContext : JsonSerializerContext
{
}