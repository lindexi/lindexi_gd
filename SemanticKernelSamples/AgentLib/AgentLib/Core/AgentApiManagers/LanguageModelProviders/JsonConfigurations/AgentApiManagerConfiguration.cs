using System.Text.Json;
#if !NET6_0
using System.Text.Json.Serialization.Metadata;
#endif

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

/// <summary>
/// API 管理器配置，包含首选模型和 OpenAI 协议配置列表。
/// </summary>
public record AgentApiManagerConfiguration
{
    /// <summary>
    /// 首选模型名称或 ID。
    /// </summary>
    public string? PrimaryModel { get; init; }

    /// <summary>
    /// OpenAI 协议语言模型配置列表。
    /// </summary>
    public IReadOnlyList<OpenAIProtocolLanguageModelConfiguration>? OpenAIConfigurationList { get; init; }

    /// <summary>
    /// 将配置异步保存到 JSON 文件。
    /// </summary>
    /// <param name="file">目标文件。</param>
    /// <returns>表示异步操作的任务。</returns>
    public async Task SaveToFileAsync(FileInfo file)
    {
        await using var fileStream = file.OpenWrite();
#if NET6_0
        await JsonSerializer.SerializeAsync(fileStream, this);
#else
        await JsonSerializer.SerializeAsync(fileStream, this, JsonTypeInfo);
#endif
    }

    /// <summary>
    /// 从 JSON 文件异步加载配置。
    /// </summary>
    /// <param name="file">JSON 配置文件。</param>
    /// <returns>配置实例。</returns>
    public static async Task<AgentApiManagerConfiguration> FromJsonFileAsync(FileInfo file)
    {
        await using var fileStream = file.OpenRead();
#if NET6_0
        var configuration = await JsonSerializer.DeserializeAsync<AgentApiManagerConfiguration>(fileStream).ConfigureAwait(false);
#else
        var configuration = await JsonSerializer.DeserializeAsync(fileStream, JsonTypeInfo).ConfigureAwait(false);
#endif
        return FromConfiguration(configuration);
    }

    /// <summary>
    /// 从 JSON 字符串反序列化配置。
    /// </summary>
    /// <param name="json">JSON 字符串。</param>
    /// <returns>配置实例。</returns>
    public static AgentApiManagerConfiguration FromJsonString(string json)
    {
#if NET6_0
        var configuration = JsonSerializer.Deserialize<AgentApiManagerConfiguration>(json);
#else
        var configuration = JsonSerializer.Deserialize(json, JsonTypeInfo);
#endif
        return FromConfiguration(configuration);
    }

    /// <summary>
    /// 从 <see cref="JsonElement"/> 反序列化配置。
    /// </summary>
    /// <param name="jsonElement">JSON 元素。</param>
    /// <returns>配置实例。</returns>
    public static AgentApiManagerConfiguration FromJsonElement(JsonElement jsonElement)
    {
#if NET6_0
        var configuration = jsonElement.Deserialize<AgentApiManagerConfiguration>();
#else
        var configuration = jsonElement.Deserialize(JsonTypeInfo);
#endif
        return FromConfiguration(configuration);
    }

    private static AgentApiManagerConfiguration FromConfiguration(AgentApiManagerConfiguration? configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration;
    }

#if !NET6_0
        private static JsonTypeInfo<AgentApiManagerConfiguration> JsonTypeInfo => JsonConfigurationOpenAIProtocolLanguageModelJsonSerializerContext.Default.AgentApiManagerConfiguration;
#endif

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