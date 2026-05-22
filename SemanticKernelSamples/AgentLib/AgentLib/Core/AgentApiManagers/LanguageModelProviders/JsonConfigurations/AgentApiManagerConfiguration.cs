using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

public record AgentApiManagerConfiguration
{
    public string? PrimaryModel { get; init; }

    public IReadOnlyList<OpenAIProtocolLanguageModelConfiguration>? OpenAIConfigurationList { get; init; }

    public static async Task<AgentApiManagerConfiguration> FromJsonFileAsync(FileInfo file)
    {
        await using var fileStream = file.OpenRead();
        var configuration = await JsonSerializer.DeserializeAsync(fileStream, JsonTypeInfo);
        return FromConfiguration(configuration);
    }

    public static AgentApiManagerConfiguration FromJsonString(string json)
    {
        var configuration = JsonSerializer.Deserialize(json, JsonTypeInfo);
        return FromConfiguration(configuration);
    }

    public static AgentApiManagerConfiguration FromJsonElement(JsonElement jsonElement)
    {
        var configuration = jsonElement.Deserialize(JsonTypeInfo);
        return FromConfiguration(configuration);
    }

    private static AgentApiManagerConfiguration FromConfiguration(AgentApiManagerConfiguration? configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration;
    }

    private static JsonTypeInfo<AgentApiManagerConfiguration> JsonTypeInfo=> JsonConfigurationOpenAIProtocolLanguageModelJsonSerializerContext.Default.AgentApiManagerConfiguration;
}