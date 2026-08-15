using System.ComponentModel;
using System.Text.Json;

using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Model;
using AgentLib.Tools;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001

namespace AgentLib.Coding;

/// <summary>
/// 为编程智能体提供独立的图片分析子智能体工具。
/// </summary>
public sealed class CodingImageSubAgentTool : ICodingWorkspaceToolSource
{
    internal const string ToolName = "AnalyzeImage";
    internal const string SubmitResultToolName = "SubmitImageAnalysisResult";

    private const string DisplayName = "分析图片";
    private const string MissingSubmissionResult = "图片分析子智能体未调用 SubmitImageAnalysisResult 提交最终结果。";
    private const string RequireSubmissionPrompt = "你尚未调用 SubmitImageAnalysisResult。必须立即调用该工具提交最终完整结论；普通文本不会返回给上一级智能体。";
    private const string ImageModePrompt = """
        你当前运行在图片读取子智能体工具模式中。
        你的任务是严格按照用户要求分析随用户消息提供的图片。
        图片是当前任务的主要输入，不要假设未在图片中出现的信息。
        完成分析后，必须调用 SubmitImageAnalysisResult 工具提交最终完整结论。
        不要仅通过普通助手文本返回最终结果；只有提交工具中的内容会返回给上一级智能体。
        """;

    private readonly AgentApiEndpointManager _agentApiEndpointManager;

    /// <summary>
    /// 创建图片分析工具提供器。
    /// </summary>
    /// <param name="agentApiEndpointManager">API 终结点管理器。</param>
    public CodingImageSubAgentTool(AgentApiEndpointManager agentApiEndpointManager)
    {
        ArgumentNullException.ThrowIfNull(agentApiEndpointManager);
        _agentApiEndpointManager = agentApiEndpointManager;
    }

    /// <inheritdoc />
    public IReadOnlyList<AITool> CreateTools(string workspacePath) => [];

    /// <inheritdoc />
    public IReadOnlyList<ToolRegistration> CreateRunToolRegistrations(
        string workspacePath,
        CopilotChatMessage assistantChatMessage)
    {
        ArgumentNullException.ThrowIfNull(assistantChatMessage);
        var executor = new CodingImageSubAgentExecutor(
            _agentApiEndpointManager,
            assistantChatMessage);

        return
        [
            new ToolRegistration(
                AIFunctionFactory.Create(
                    executor.AnalyzeImageAsync,
                    ToolName,
                    "使用独立的多模态子智能体读取并分析一张或多张图片。适用于截图、设计稿、流程图、架构图和图片文字提取。输入图片路径数组和明确的分析指令，返回子智能体提交的最终文本结论。"),
                CreatePresentation),
        ];
    }

    private sealed class CodingImageSubAgentExecutor
    {
        private readonly AgentApiEndpointManager _agentApiEndpointManager;
        private readonly CopilotChatMessage _assistantChatMessage;

        internal CodingImageSubAgentExecutor(
            AgentApiEndpointManager agentApiEndpointManager,
            CopilotChatMessage assistantChatMessage)
        {
            _agentApiEndpointManager = agentApiEndpointManager;
            _assistantChatMessage = assistantChatMessage;
        }

        [Description("使用独立的多模态子智能体读取并分析一张或多张图片。")]
        internal async Task<string> AnalyzeImageAsync(
            [Description("图片文件路径数组，可包含一个或多个文件。")]
            string[] filePath,
            [Description("给子智能体的分析指令，明确需要观察、提取、比较或判断的内容，以及期望的输出格式。")]
            string analysisInstruction,
            CancellationToken cancellationToken = default)
        {
            if (filePath is not { Length: > 0 } || filePath.Any(string.IsNullOrWhiteSpace))
            {
                return "无法分析图片：图片文件路径数组不能为空，且不能包含空路径。";
            }

            if (string.IsNullOrWhiteSpace(analysisInstruction))
            {
                return "无法分析图片：图片分析指令不能为空。";
            }

            var imageContents = new List<DataContent>(filePath.Length);
            foreach (string path in filePath)
            {
                try
                {
                    DataContent content = await DataContent.LoadFromAsync(path, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    if (content.MediaType is null
                        || !content.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"无法分析图片：文件“{path}”不是受支持的图片，请提供 PNG、JPG 等常见图片格式。";
                    }

                    imageContents.Add(content);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
                {
                    return $"无法读取图片“{path}”：{exception.Message}";
                }
            }

            IChatClient chatClient;
            try
            {
                chatClient = await _agentApiEndpointManager.PrimaryModel
                    .GetChatClientAsync()
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return $"无法启动图片分析子智能体：{exception.Message}";
            }

            var submissionTool = new GeneratedTextSubmissionTool();
            AIFunction submitFunction = submissionTool.CreateTool(
                SubmitResultToolName,
                "提交根据分析指令和图片得到的最终完整结论。");
            ChatClientAgent chatClientAgent = chatClient.AsAIAgent(new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions
                {
                    Tools = [submitFunction],
                },
                ChatHistoryProvider = null,
                AIContextProviders = [],
                RequirePerServiceCallChatHistoryPersistence = true,
            });
            AgentSession agentSession = await chatClientAgent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
            CodingSystemPrompt.EnsureInitialized(agentSession);
            agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? messages);
            messages ??= [];
            messages.Add(new ChatMessage(ChatRole.System, ImageModePrompt));
            agentSession.SetInMemoryChatHistory(messages);

            List<AIContent> userContents =
            [
                new TextContent($"请根据以下分析指令处理随本消息提供的图片：{analysisInstruction}"),
                .. imageContents,
            ];
            CopilotChatSubAgentItem subAgentItem = _assistantChatMessage.CreateSubAgentItem(
                DisplayName,
                $"{string.Join("；", filePath)}\n{analysisInstruction}");

            async Task RunAsync(IReadOnlyList<ChatMessage> inputMessages)
            {
                await foreach (AgentResponseUpdate update in chatClientAgent.RunWithHistoryCompletionAsync(
                                   inputMessages,
                                   agentSession,
                                   cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (AIContent content in update.Contents)
                    {
                        switch (content)
                        {
                            case TextReasoningContent reasoning when !string.IsNullOrEmpty(reasoning.Text):
                                subAgentItem.AppendReasoning(reasoning.Text);
                                break;
                            case TextContent text when !string.IsNullOrEmpty(text.Text):
                                subAgentItem.AppendText(text.Text);
                                break;
                            case FunctionCallContent functionCall:
                                subAgentItem.AppendFunctionCall(functionCall);
                                break;
                            case FunctionResultContent functionResult:
                                subAgentItem.AppendFunctionResult(functionResult);
                                break;
                        }
                    }
                }
            }

            try
            {
                await RunAsync([new ChatMessage(ChatRole.User, userContents)]).ConfigureAwait(false);
                if (!submissionTool.HasSubmittedText)
                {
                    await RunAsync([new ChatMessage(ChatRole.User, RequireSubmissionPrompt)]).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return $"图片分析子智能体执行失败：{exception.Message}";
            }

            return submissionTool.HasSubmittedText
                ? submissionTool.SubmittedText!
                : MissingSubmissionResult;
        }
    }

    private static ToolCallPresentation CreatePresentation(IDictionary<string, object?> arguments)
    {
        string? instruction = ToolCallPresentationFactory.GetString(arguments, "analysisInstruction");
        return new ToolCallPresentation(GetFilePathSummary(arguments), instruction, null);
    }

    private static string? GetFilePathSummary(IDictionary<string, object?> arguments)
    {
        if (!arguments.TryGetValue("filePath", out object? value) || value is null)
        {
            return null;
        }

        IEnumerable<string> paths = value switch
        {
            string[] array => array,
            IEnumerable<string> enumerable => enumerable,
            JsonElement { ValueKind: JsonValueKind.Array } element => element.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path!),
            _ => [],
        };
        string[] pathArray = paths.ToArray();
        return pathArray.Length == 0 ? null : string.Join("；", pathArray);
    }
}
