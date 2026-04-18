using System.ClientModel;
using System.ComponentModel;
using System.Text;

using LeefayjehekijawlalWhichayfawcelhega.Models;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI;

namespace LeefayjehekijawlalWhichayfawcelhega.Services;

internal sealed class AiPromptAgentService
{
    private const string PromptInstructions = "你是一名专业的课件分页与视觉提示词助手。你接收到的是完整的大纲，不是已经拆好的页面。你需要先判断应该拆成多少页，再为每一页生成适合文生图模型直接使用的中文提示词。你必须调用工具提交结果，不要直接在对话正文输出提示词。";

    private readonly AiServiceOptions _options;

    public AiPromptAgentService(AiServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public async Task<IReadOnlyList<string>> GeneratePromptsAsync(string outlineText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(outlineText))
        {
            throw new ArgumentException("大纲内容不能为空。", nameof(outlineText));
        }

        cancellationToken.ThrowIfCancellationRequested();

        OpenAIClient openAiClient = CreateClient();
        List<string>? generatedPrompts = null;
        StringBuilder fallbackOutputBuilder = new();

        [Description("提交逐页图片提示词列表。")]
        string SubmitSlidePrompts([Description("逐页图片提示词列表。每个字符串对应一页，必须保持页面顺序。")]
            List<string> slidePrompts)
        {
            if (slidePrompts is null || slidePrompts.Count == 0)
            {
                throw new ArgumentException("逐页提示词列表不能为空。", nameof(slidePrompts));
            }

            List<string> normalizedPrompts = slidePrompts
                .Where(prompt => !string.IsNullOrWhiteSpace(prompt))
                .Select(NormalizePrompt)
                .Where(prompt => !string.IsNullOrWhiteSpace(prompt))
                .ToList();

            if (normalizedPrompts.Count == 0)
            {
                throw new InvalidOperationException("模型提交的逐页提示词全部为空。");
            }

            generatedPrompts = normalizedPrompts;
            return $"已接收 {normalizedPrompts.Count} 页提示词。";
        }

        AIAgent agent = openAiClient
            .GetChatClient(_options.PromptModelId)
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation(
                loggerFactory: null,
                configure: functionInvokingChatClient =>
                {
                    functionInvokingChatClient.FunctionInvoker = (context, token) =>
                    {
                        context.Terminate = true;
                        return context.Function.InvokeAsync(context.Arguments, token);
                    };
                })
            .BuildAIAgent(new ChatClientAgentOptions
            {
                ChatOptions = new()
                {
                    Instructions = PromptInstructions,
                    Tools =
                    [
                        AIFunctionFactory.Create(SubmitSlidePrompts),
                    ],
                },
            });

        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(BuildSlidePrompt(outlineText)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(update.Text))
            {
                fallbackOutputBuilder.Append(update.Text);
            }
        }

        if (generatedPrompts is null || generatedPrompts.Count == 0)
        {
            string fallbackOutput = NormalizePrompt(fallbackOutputBuilder.ToString());
            if (string.IsNullOrWhiteSpace(fallbackOutput))
            {
                throw new InvalidOperationException("模型没有通过工具返回可用的逐页提示词。请检查模型配置后重试。");
            }

            throw new InvalidOperationException($"模型没有调用工具返回逐页提示词，模型输出为：{fallbackOutput}");
        }

        return generatedPrompts;
    }

    private string BuildSlidePrompt(string outlineText)
    {
        return $$"""
请阅读以下完整的 PPT 大纲，并自行决定应该拆分成多少页。

你的任务：
1. 基于整份大纲判断页面边界，而不是依赖一行一页或空行一页。
2. 为每一页生成一段可直接用于文生图模型的中文提示词。
3. 提示词需要体现主体、构图、风格、色彩、光线和关键细节。
4. 如果某一页内容偏抽象，请主动补足合适的视觉化表达。
5. 必须调用工具 `SubmitSlidePrompts` 一次性提交全部结果。
6. 工具参数必须是字符串列表，并且按照一个页面一个字符串的方式传入。
7. 不要在对话正文输出解释、标题、编号、markdown 或提示词正文。

完整大纲：
{{outlineText}}
""";
    }

    private OpenAIClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new InvalidOperationException("服务 Endpoint 不能为空。请先在界面中配置服务地址。");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("API Key 不能为空。请先在界面中输入 API Key。");
        }

        if (string.IsNullOrWhiteSpace(_options.PromptModelId))
        {
            throw new InvalidOperationException("提示词模型不能为空。请先在界面中配置提示词模型。");
        }

        return new OpenAIClient(
            new ApiKeyCredential(_options.ApiKey.Trim()),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(_options.Endpoint.Trim()),
            });
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
