using System.ClientModel;
using System.ComponentModel;

using LeefayjehekijawlalWhichayfawcelhega.Models;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;

namespace LeefayjehekijawlalWhichayfawcelhega.Services;

internal sealed class PromptAgentService
{
    private const string PromptInstructions = "你是一名专业的课件视觉设计助手。你的任务是阅读完整 PPT 大纲，自主决定应该拆成多少页，并为每一页生成一段适合文生图模型直接使用的中文提示词。你必须调用工具提交逐页结果，不要直接把结果输出到对话文本。";

    private readonly AiProviderOptions _options;

    public PromptAgentService(AiProviderOptions options)
    {
        _options = options;
    }

    public async Task<IReadOnlyList<string>> GeneratePromptsAsync(string outlineText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outlineText))
        {
            throw new ArgumentException("大纲内容不能为空。", nameof(outlineText));
        }

        cancellationToken.ThrowIfCancellationRequested();

        List<string>? prompts = null;

        [Description("提交已经按页拆分完成的图片提示词结果。必须按页面顺序调用，列表中的每一个字符串代表一页的图片提示词。调用一次即可完成任务。")]
        string SubmitSlidePrompts([Description("逐页图片提示词列表。一个字符串就是一页，必须保持顺序。不要把多页内容合并到一个字符串里。")]
            List<string> slidePrompts)
        {
            ArgumentNullException.ThrowIfNull(slidePrompts);

            prompts = slidePrompts
                .Select(NormalizePrompt)
                .Where(prompt => !string.IsNullOrWhiteSpace(prompt))
                .ToList();

            return $"已记录 {prompts.Count} 页图片提示词。";
        }

        OpenAIClient openAiClient = CreateOpenAiClient();

        AIAgent agent = openAiClient
            .GetChatClient(_options.PromptModelId)
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation(configure: functionInvokingChatClient =>
            {
                functionInvokingChatClient.FunctionInvoker = (context, token) =>
                {
                    context.Terminate = true;
                    return context.Function.InvokeAsync(context.Arguments, token);
                };
            })
            .BuildAIAgent(options: new ChatClientAgentOptions
            {
                ChatOptions = new()
                {
                    Instructions = PromptInstructions,
                    Tools = [AIFunctionFactory.Create(SubmitSlidePrompts)],
                },
            });

        await foreach (AgentResponseUpdate _ in agent.RunStreamingAsync(BuildSlidePrompt(outlineText)).WithCancellation(cancellationToken))
        {
        }

        if (prompts is null || prompts.Count == 0)
        {
            throw new InvalidOperationException("模型没有通过工具返回可用的逐页图片提示词。");
        }

        return prompts;
    }

    private OpenAIClient CreateOpenAiClient()
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new InvalidOperationException("请先在界面配置模型服务地址。");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("请先在界面配置 API Key。");
        }

        return new OpenAIClient(
            new ApiKeyCredential(_options.ApiKey.Trim()),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(_options.Endpoint.Trim()),
            });
    }

    private string BuildSlidePrompt(string outlineText)
    {
        return $$"""
请阅读以下完整的 PPT 大纲，并自行判断应该拆成多少页。

你的任务：
1. 基于整个大纲决定分页，而不是按输入行数机械拆分。
2. 为每一页生成一段适合文生图模型直接使用的中文提示词。
3. 每一页对应一个字符串。
4. 你必须调用提供的工具提交结果。
5. 调用工具时，字符串列表需要严格按照页面顺序传入。
6. 不要在对话文本里直接输出提示词，不要输出解释、标题、序号或 markdown。

完整大纲：
{{outlineText}}
""";
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
