using System.Text.Json.Serialization;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified, WriteIndented = true)]
[JsonSerializable(typeof(OpenAIProtocolLanguageModelConfiguration))]
[JsonSerializable(typeof(AgentApiManagerConfiguration))]
public partial class JsonConfigurationOpenAIProtocolLanguageModelJsonSerializerContext : JsonSerializerContext
{
}