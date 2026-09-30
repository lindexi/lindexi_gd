using System.Text.Json.Serialization;

namespace XiaoXiIme.Dictionary;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(UserDictionaryEntry))]
[JsonSerializable(typeof(UserDictionaryEntry[]))]
[JsonSerializable(typeof(UserDictionaryStore.UserDictionaryDocument))]
internal sealed partial class UserDictionaryJsonSerializerContext : JsonSerializerContext
{
}
