using System.Text.Json.Serialization;

namespace HefairkallkelemKaylowaiwi;

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}