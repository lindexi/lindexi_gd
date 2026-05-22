using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AgentLib.Core.AgentApiManagers.Contexts;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

public class JsonConfigurationOpenAIProtocolLanguageModelProvider(OpenAIProtocolLanguageModelConfiguration configuration) : OpenAIProtocolLanguageModelProviderBase(configuration.EndPoint, configuration.Key)
{
    protected override IReadOnlyList<ModelDefinition> GetModelDefinitions()
    {
        return _configuration.ModelDefinitions ?? [];
    }

    private readonly OpenAIProtocolLanguageModelConfiguration _configuration = configuration;

    public static async Task<JsonConfigurationOpenAIProtocolLanguageModelProvider> FromJsonFileAsync(FileInfo file)
    {
        await using var fileStream = file.OpenRead();
        var configuration = await JsonSerializer.DeserializeAsync(fileStream, JsonTypeInfo);
        return FromConfiguration(configuration);
    }

    public static JsonConfigurationOpenAIProtocolLanguageModelProvider FromJsonString(string json)
    {
        var configuration = JsonSerializer.Deserialize(json, JsonTypeInfo);
        return FromConfiguration(configuration);
    }

    public static JsonConfigurationOpenAIProtocolLanguageModelProvider FromJsonElement(JsonElement jsonElement)
    {
        var configuration = jsonElement.Deserialize(JsonTypeInfo);
        return FromConfiguration(configuration);
    }

    public static JsonConfigurationOpenAIProtocolLanguageModelProvider FromConfiguration(OpenAIProtocolLanguageModelConfiguration? configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new JsonConfigurationOpenAIProtocolLanguageModelProvider(configuration);
    }

    private static JsonTypeInfo<OpenAIProtocolLanguageModelConfiguration> JsonTypeInfo => JsonConfigurationOpenAIProtocolLanguageModelJsonSerializerContext.Default.OpenAIProtocolLanguageModelConfiguration;
}