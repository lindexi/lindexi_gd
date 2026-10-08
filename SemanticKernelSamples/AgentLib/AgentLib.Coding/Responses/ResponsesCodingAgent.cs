using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace AgentLib.Coding;

/// <summary>
/// 使用原生 Responses 流式协议和共享工作区工具执行编程任务。
/// </summary>
public sealed class ResponsesCodingAgent
{
    private readonly CodingAgent _codingAgent;
    private readonly string? _copilotInstructionsPath;

    /// <summary>
    /// 创建执行引擎；共享工作区资源的生命周期由宿主负责。
    /// </summary>
    public ResponsesCodingAgent(CodingAgent codingAgent, string? copilotInstructionsPath = null)
    {
        ArgumentNullException.ThrowIfNull(codingAgent);
        _codingAgent = codingAgent;
        _copilotInstructionsPath = copilotInstructionsPath;
    }

    /// <summary>
    /// 启动流式运行，使用调用方持有的对话历史；不迁移 Chat 协议状态。
    /// </summary>
    public async Task<CodingAgentRunResult> RunAsync(IManualSendMessageContext context,
        IReadOnlyList<AIContent> contents, CopilotResponsesSession conversation,
        string? workspacePath, bool enableDotNetRun = false,
        ReasoningEffort? reasoningEffort = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(conversation);
        if (contents.Count == 0)
        {
            throw new ArgumentException("编程任务内容不能为空。", nameof(contents));
        }

        var input = ResponseItem.CreateUserMessageItem(contents.Select(CreateInputPart));
        var prompts = await CodingPromptProvider.BuildAsync(_copilotInstructionsPath, cancellationToken).ConfigureAwait(false);
        var client = await ((IResponsesClientProvider)context.LanguageModel)
            .GetResponsesClientAsync().ConfigureAwait(false);
        var workspaceContext = await _codingAgent.GetRunWorkspaceContextAsync(workspacePath, enableDotNetRun, cancellationToken).ConfigureAwait(false);
        var runner = new ResponsesCodingAgentRunner
        {
            MessageContext = context,
            Contents = contents,
            Conversation = conversation,
            Input = input,
            Instructions = string.Join(Environment.NewLine + Environment.NewLine, prompts),
            Client = client,
            Workspace = workspaceContext,
            CancellationToken = cancellationToken,
            ReasoningEffort = reasoningEffort,
        };
        return new CodingAgentRunResult(context.AssistantChatMessage, runner.RunAsync());
    }

    private static ResponseContentPart CreateInputPart(AIContent content) => content switch
    {
        TextContent text => ResponseContentPart.CreateInputTextPart(text.Text),
        DataContent image when image.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) =>
            ResponseContentPart.CreateInputImagePart(image.Uri),
        UriContent image when image.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) =>
            ResponseContentPart.CreateInputImagePart(image.Uri),
        _ => throw new NotSupportedException($"Responses 编程输入不支持 {content.GetType().Name}。"),
    };
}
