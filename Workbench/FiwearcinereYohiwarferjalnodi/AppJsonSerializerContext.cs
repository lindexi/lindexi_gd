using System.Text.Json.Serialization;

namespace FiwearcinereYohiwarferjalnodi;

[JsonSerializable(typeof(IComponent))]
[JsonSerializable(typeof(Foo))]
[JsonSourceGenerationOptions(WriteIndented = true)]
partial class AppJsonSerializerContext : JsonSerializerContext
{
}