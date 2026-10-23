using AgentLib.Core.AgentApiManagers.Contexts;

namespace AgentLib.Core.AgentApiManagers.LanguageModelProviders;

/// <summary>
/// DeepSeek 协议语言模型提供商，提供 DeepSeek 系列模型定义。
/// </summary>
public class DeepSeekProtocolLanguageModelProvider(string endPoint, string key) : OpenAIProtocolLanguageModelProviderBase(endPoint, key)
{
    protected override IReadOnlyList<ModelDefinition> GetModelDefinitions()
    {
        return
        [
            new ModelDefinition
            {
                Provider = "deepseek",
                ModelName = "deepseek-v4-pro",
                Capabilities = new LlmModelCapabilities
                {
                    Reasoning = true,
                    ToolCall = true,
                    Temperature = true,
                    Input = new LlmModalityCapability { Text = true, Image = false },
                },
                ContextWindowSize = 1_000_000,
                MaxOutputTokens = 393_216,
            },
            new ModelDefinition
            {
                Provider = "deepseek",
                ModelName = "deepseek-v4-flash",
                Capabilities = new LlmModelCapabilities
                {
                    Reasoning = true,
                    ToolCall = true,
                    Temperature = true,
                    Input = new LlmModalityCapability { Text = true, Image = false },
                    IsFlash = true,
                },
                ContextWindowSize = 1_000_000,
                MaxOutputTokens = 393_216,
            },
        ];
    }
}