using System.ClientModel;
using System.Text;

using LeefayjehekijawlalWhichayfawcelhega.Models;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;

namespace LeefayjehekijawlalWhichayfawcelhega.Services;

internal sealed class DoubaoPromptAgentService
{
    private const string PromptInstructions = "你是一名专业的课件视觉设计助手。你只输出单页课件图片生成提示词，不要输出解释、标题、编号、markdown、引号或额外说明。";

    private readonly DoubaoOptions _options;

    public DoubaoPromptAgentService(DoubaoOptions options)
    {
        _options = options;
    }

    public async Task<string> GeneratePromptAsync(string slideContent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slideContent))
        {
            throw new ArgumentException("页面内容不能为空。", nameof(slideContent));
        }

        cancellationToken.ThrowIfCancellationRequested();

        string apiKey = GetApiKey();
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(_options.Endpoint),
            });

        AIAgent agent = openAiClient
            .GetChatClient(_options.PromptModelId)
            .AsIChatClient()
            .AsAIAgent(new ChatClientAgentOptions
            {
                ChatOptions = new()
                {
                    Instructions = PromptInstructions,
                },
            });

        StringBuilder promptBuilder = new();

        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(BuildSlidePrompt(slideContent)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(update.Text))
            {
                promptBuilder.Append(update.Text);
            }
        }

        string prompt = NormalizePrompt(promptBuilder.ToString());
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new InvalidOperationException("模型没有返回可用的图片提示词。");
        }

        return prompt;
    }

    private string BuildSlidePrompt(string slideContent)
    {
        return $$"""
请根据以下单页 PPT 内容，生成一段适合文生图模型直接使用的中文提示词。

要求：
1. 只输出最终提示词正文。
2. 画面要适合作为 PPT 页面主视觉。
3. 需要体现画面主体、构图、风格、色彩、光线和细节。
4. 不要输出解释、标题、序号或 markdown。
5. 如果页面内容偏抽象，请补足合适的视觉化表达。

单页内容：
{{slideContent}}
""";
    }

    private string GetApiKey()
    {
        string? apiKey = Environment.GetEnvironmentVariable(_options.ApiKeyEnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException($"环境变量 {_options.ApiKeyEnvironmentVariableName} 未设置。请先配置豆包 API Key。");
        }

        return apiKey.Trim();
    }

    private static string NormalizePrompt(string prompt)
    {
        string normalized = prompt.Trim();

        if (normalized.StartsWith("```", StringComparison.Ordinal))
        {
            normalized = normalized.Trim('`').Trim();
        }

        return normalized.Trim().Trim('"');
    }
}
