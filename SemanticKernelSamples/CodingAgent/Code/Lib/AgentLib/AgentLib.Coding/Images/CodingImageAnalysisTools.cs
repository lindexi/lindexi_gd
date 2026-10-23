using System.ComponentModel;
using AgentLib.Model;
using AgentLib.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Images;

internal sealed class CodingImageAnalysisTools
{
    internal const string AnalyzeImageToolName = "AnalyzeImage";
    internal const string SubmitImageAnalysisResultToolName = "SubmitImageAnalysisResult";
    internal const string MissingSubmissionResult = "图片分析子智能体已重试一次，但仍未调用 SubmitImageAnalysisResult 工具提交分析结果。";

    private const string ImageAnalysisSystemPrompt
        = """
          你当前运行在图片读取子智能体工具模式中。
          你的任务是严格按照用户要求分析随用户消息提供的图片。
          图片是当前任务的主要输入，不要假设未在图片中出现的信息。
          完成分析后，必须调用 SubmitImageAnalysisResult 工具提交最终完整结论。
          不要仅通过普通助手文本返回最终结果；只有提交工具中的内容会返回给上一级智能体。
          """;

    private const string RequireSubmissionPrompt = "你尚未调用 SubmitImageAnalysisResult。必须立即调用该工具提交最终完整结论；普通文本不会返回给上一级智能体。";

    private readonly CopilotChatManager _chatManager;

    internal CodingImageAnalysisTools(CopilotChatManager chatManager)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        _chatManager = chatManager;
    }

    internal IReadOnlyList<ToolRegistration> AsToolRegistrations() =>
    [
        new
        (
            AIFunctionFactory.Create
            (
                AnalyzeImageAsync,
                AnalyzeImageToolName,
                "使用独立的多模态子智能体读取并分析一张或多张图片。适用于截图、设计稿、流程图、架构图和图片文字提取。输入图片路径数组和明确的分析指令，返回子智能体提交的最终文本结论。"
            ),
            CreatePresentation
        )
    ];

    [Description("使用独立的多模态子智能体读取并分析一张或多张图片。")]
    internal async Task<string> AnalyzeImageAsync
    (
        [Description("要分析的图片文件路径列表。路径按当前进程的文件路径语义直接读取。")]
        IReadOnlyList<string> filePath,
        [Description("给图片分析子智能体的明确要求，包括需要观察、提取、比较或判断的内容以及期望输出格式。")]
        string analysisInstruction,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(analysisInstruction))
            {
                return "图片分析失败：图片分析要求不能为空。";
            }

            var userContents = new List<AIContent>(filePath.Count + 1)
            {
                new TextContent($"请根据以下分析指令处理随本消息提供的图片：{analysisInstruction}"),
            };
            foreach (string path in filePath)
            {
                DataContent image = await DataContent.LoadFromAsync
                        (path, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (!image.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return $"图片分析失败：文件“{path}”不是受支持的图片，请提供 PNG、JPG 等常见图片格式。";
                }

                userContents.Add(image);
            }

            IManualSendMessageContext context = await _chatManager
                .CreateManualSendMessageContextAsync(cancellationToken)
                .ConfigureAwait(false);
            var submission = new GeneratedTextSubmissionTool();
            AIFunction submissionTool = submission.CreateTool
            (
                SubmitImageAnalysisResultToolName,
                "提交根据分析要求和全部图片得到的最终完整结论。必须调用此工具返回结果。仅提交最终结论，不要附加工具调用说明。"
            );
            ChatClientAgent agent = await context.GetChatClientAgentAsync
            (
                options =>
                {
                    options.ChatOptions ??= new ChatOptions();
                    options.ChatOptions.Tools = [submissionTool];
                    options.AIContextProviders = [];
                    options.EnableMessageInjection = false;
                    options.ChatHistoryProvider = null;
                    options.RequirePerServiceCallChatHistoryPersistence = false;
                },
                cancellationToken
            ).ConfigureAwait(false);

            List<ChatMessage> messages =
            [
                new(ChatRole.System, ImageAnalysisSystemPrompt),
                new(ChatRole.User, userContents),
            ];
            await RunOnceAsync(agent, messages, cancellationToken).ConfigureAwait(false);
            if (!submission.HasSubmittedText)
            {
                messages.Add(new ChatMessage(ChatRole.User, RequireSubmissionPrompt));
                await RunOnceAsync(agent, messages, cancellationToken).ConfigureAwait(false);
            }

            return submission.SubmittedText ?? MissingSubmissionResult;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return exception.ToString();
        }
    }

    private static async Task RunOnceAsync
    (
        ChatClientAgent agent,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken
    )
    {
        await foreach (AgentResponseUpdate _ in agent.RunStreamingAsync
                       (
                           messages,
                           cancellationToken: cancellationToken
                       ).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private static ToolCallPresentation CreatePresentation(IDictionary<string, object?> arguments)
    {
        string? instruction = ToolCallPresentationFactory.GetString(arguments, "analysisInstruction")
                              ?? ToolCallPresentationFactory.GetString(arguments, "AnalysisInstruction");
        return new ToolCallPresentation("分析图片", instruction, null);
    }
}