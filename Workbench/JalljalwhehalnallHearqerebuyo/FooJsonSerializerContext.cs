using System.Text.Json.Serialization;

namespace JalljalwhehalnallHearqerebuyo;

[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(F1))]
partial class FooJsonSerializerContext : JsonSerializerContext;