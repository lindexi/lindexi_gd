using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified, WriteIndented = true, ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(OpenAIProtocolLanguageModelConfiguration))]
[JsonSerializable(typeof(AgentApiManagerConfiguration))]
public partial class JsonConfigurationOpenAIProtocolLanguageModelJsonSerializerContext : JsonSerializerContext
{
}