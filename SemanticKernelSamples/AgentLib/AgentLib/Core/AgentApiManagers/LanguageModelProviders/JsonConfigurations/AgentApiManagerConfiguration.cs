using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

public record AgentApiManagerConfiguration
{
    public string? PrimaryModel { get; init; }

    public IReadOnlyList<OpenAIProtocolLanguageModelConfiguration>? OpenAIConfigurationList { get; init; }

    public async Task SaveToFileAsync(FileInfo file)
    {
        await using var fileStream = file.OpenWrite();
        await JsonSerializer.SerializeAsync(fileStream,this, JsonTypeInfo);
    }

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

    /// <summary>
    /// 默认的模版内容
    /// </summary>
    public const string DefaultTemplateFileContent =
        """
        {
          "PrimaryModel": "", // <-- Here. Your Primary Model Name, e.g. "deepseek-v4-pro"
          "OpenAIConfigurationList": [
            {
              "EndPoint": "https://api.deepseek.com",
              "Key": "", // <-- Here. Your DeepSeek Key
              "ModelDefinitions": [
                {
                  "Provider": "deepseek",
                  "ModelName": "deepseek-v4-pro",
                  "ModelId": null,
                  "Capabilities": {
                    "Temperature": true,
                    "Reasoning": true,
                    "Attachment": false,
                    "ToolCall": true,
                    "Input": {
                      "Text": true,
                      "Image": false,
                      "Audio": false,
                      "Video": false,
                      "Pdf": false
                    },
                    "Output": {
                      "Text": true,
                      "Image": false,
                      "Audio": false,
                      "Video": false,
                      "Pdf": false
                    },
                    "Interleaved": false,
                    "IsFlash": false,
                    "ResponseFormat": false
                  },
                  "ContextWindowSize": 1000000,
                  "MaxOutputTokens": 393216
                },
                {
                  "Provider": "deepseek",
                  "ModelName": "deepseek-v4-flash",
                  "ModelId": null,
                  "Capabilities": {
                    "Temperature": true,
                    "Reasoning": true,
                    "Attachment": false,
                    "ToolCall": true,
                    "Input": {
                      "Text": true,
                      "Image": false,
                      "Audio": false,
                      "Video": false,
                      "Pdf": false
                    },
                    "Output": {
                      "Text": true,
                      "Image": false,
                      "Audio": false,
                      "Video": false,
                      "Pdf": false
                    },
                    "Interleaved": false,
                    "IsFlash": true,
                    "ResponseFormat": false
                  },
                  "ContextWindowSize": 1000000,
                  "MaxOutputTokens": 393216
                }
              ]
            },
            {
              "EndPoint": "https://ark.cn-beijing.volces.com/api/v3",
              "Key": "", // <-- Here. Your DouBao Key
              "ModelDefinitions": [
                {
                  "Provider": "Doubao",
                  "ModelName": "Doubao-Seed-2.0-pro",
                  "ModelId": "", // <-- Here. Your Model Id, e.g. "ep-20260306101224-cadtg"
                  "Capabilities": {
                    "Temperature": false,
                    "Reasoning": true,
                    "Attachment": false,
                    "ToolCall": true,
                    "Input": {
                      "Text": true,
                      "Image": true,
                      "Audio": false,
                      "Video": true,
                      "Pdf": false
                    },
                    "Output": {
                      "Text": true,
                      "Image": false,
                      "Audio": false,
                      "Video": false,
                      "Pdf": false
                    },
                    "Interleaved": false,
                    "IsFlash": false,
                    "ResponseFormat": false
                  },
                  "ContextWindowSize": null,
                  "MaxOutputTokens": null
                },
                {
                  "Provider": "Doubao",
                  "ModelName": "Doubao-Seed-2.0-lite",
                  "ModelId": "", // <-- Here. Your Model Id, e.g. "ep-20260519114607-snpl5"
                  "Capabilities": {
                    "Temperature": false,
                    "Reasoning": true,
                    "Attachment": false,
                    "ToolCall": true,
                    "Input": {
                      "Text": true,
                      "Image": true,
                      "Audio": true,
                      "Video": true,
                      "Pdf": false
                    },
                    "Output": {
                      "Text": true,
                      "Image": false,
                      "Audio": false,
                      "Video": false,
                      "Pdf": false
                    },
                    "Interleaved": false,
                    "IsFlash": true,
                    "ResponseFormat": false
                  },
                  "ContextWindowSize": null,
                  "MaxOutputTokens": null
                }
              ]
            }
          ]
        }
        """;
}