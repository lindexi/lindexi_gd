using System.Text.Json.Serialization;

namespace Lib1;

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}