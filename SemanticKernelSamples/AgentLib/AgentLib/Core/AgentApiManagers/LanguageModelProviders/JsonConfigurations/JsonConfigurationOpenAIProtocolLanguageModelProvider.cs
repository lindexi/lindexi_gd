using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AgentLib.Core.AgentApiManagers.Contexts;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

/// <summary>
/// 基于 JSON 配置的 OpenAI 协议语言模型提供商，从 <see cref="OpenAIProtocolLanguageModelConfiguration"/> 读取模型定义。
/// </summary>
public class JsonConfigurationOpenAIProtocolLanguageModelProvider(OpenAIProtocolLanguageModelConfiguration configuration) : OpenAIProtocolLanguageModelProviderBase(configuration.EndPoint, configuration.Key)
{
    protected override IReadOnlyList<ModelDefinition> GetModelDefinitions()
    {
        return _configuration.ModelDefinitions ?? [];
    }

    private readonly OpenAIProtocolLanguageModelConfiguration _configuration = configuration;

        /// <summary>
        /// 从 JSON 文件异步加载提供商配置。
        /// </summary>
        /// <param name="file">JSON 配置文件。</param>
        /// <returns>提供商实例。</returns>
        public static async Task<JsonConfigurationOpenAIProtocolLanguageModelProvider> FromJsonFileAsync(FileInfo file)
        {
            await using var fileStream = file.OpenRead();
            var configuration = await JsonSerializer.DeserializeAsync(fileStream, JsonTypeInfo);
            return FromConfiguration(configuration);
        }

        /// <summary>
        /// 从 JSON 字符串反序列化提供商配置。
        /// </summary>
        /// <param name="json">JSON 字符串。</param>
        /// <returns>提供商实例。</returns>
        public static JsonConfigurationOpenAIProtocolLanguageModelProvider FromJsonString(string json)
        {
            var configuration = JsonSerializer.Deserialize(json, JsonTypeInfo);
            return FromConfiguration(configuration);
        }

        /// <summary>
        /// 从 <see cref="JsonElement"/> 反序列化提供商配置。
        /// </summary>
        /// <param name="jsonElement">JSON 元素。</param>
        /// <returns>提供商实例。</returns>
        public static JsonConfigurationOpenAIProtocolLanguageModelProvider FromJsonElement(JsonElement jsonElement)
        {
            var configuration = jsonElement.Deserialize(JsonTypeInfo);
            return FromConfiguration(configuration);
        }

        /// <summary>
        /// 从配置对象创建提供商实例。
        /// </summary>
        /// <param name="configuration">OpenAI 协议语言模型配置。</param>
        /// <returns>提供商实例。</returns>
        /// <exception cref="ArgumentNullException">配置为 <see langword="null"/> 时抛出。</exception>
        public static JsonConfigurationOpenAIProtocolLanguageModelProvider FromConfiguration(OpenAIProtocolLanguageModelConfiguration? configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new JsonConfigurationOpenAIProtocolLanguageModelProvider(configuration);
    }

    private static JsonTypeInfo<OpenAIProtocolLanguageModelConfiguration> JsonTypeInfo => JsonConfigurationOpenAIProtocolLanguageModelJsonSerializerContext.Default.OpenAIProtocolLanguageModelConfiguration;
}