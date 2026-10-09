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
    public async Task<ICodingAgentRunResult> RunAsync(IManualSendMessageContext context,
        IReadOnlyList<AIContent> contents, CopilotResponsesSession conversation,
        string? workspacePath, CodingChatRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(conversation);
        if (contents.Count == 0)
        {
            throw new ArgumentException("编程任务内容不能为空。", nameof(contents));
        }

        CodingChatRunOptions runOptions = options ?? CodingChatRunOptions.Default;
        var input = ResponseItem.CreateUserMessageItem(contents.Select(CreateInputPart));
        var prompts = await CodingPromptProvider.BuildAsync(_copilotInstructionsPath, cancellationToken).ConfigureAwait(false);
        var client = await ((IResponsesClientProvider)context.LanguageModel)
            .GetResponsesClientAsync().ConfigureAwait(false);
        var workspaceContext = await _codingAgent.GetRunWorkspaceContextAsync(workspacePath, runOptions.EnableDotNetRun, cancellationToken).ConfigureAwait(false);
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
            ReasoningEffort = runOptions.ReasoningEffort,
            EnableAutomaticCompression = runOptions.EnableAutomaticCompression,
        };
        return new ResponsesCodingAgentRunResult(runner);
    }

    /// <summary>
    /// 通过服务端原生 Compact 压缩历史；无历史返回 false，失败或取消不修改历史且不回退。
    /// </summary>
    public async Task<bool> TryCompactConversationAsync(ILanguageModel model, CopilotResponsesSession conversation,
        string? additionalInstructions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(conversation);
        cancellationToken.ThrowIfCancellationRequested();
        if (conversation.Items.Count == 0) return false;
        var prompts = await CodingPromptProvider.BuildAsync(_copilotInstructionsPath, cancellationToken).ConfigureAwait(false);
        string instructions = string.Join(Environment.NewLine + Environment.NewLine, prompts);
        if (!string.IsNullOrWhiteSpace(additionalInstructions)) instructions += Environment.NewLine + additionalInstructions;
        var client = await ((IResponsesClientProvider)model).GetResponsesClientAsync().ConfigureAwait(false);
        return await TryCompactConversationAsync(client, model.ModelDefinition.ModelId, conversation,
            instructions, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<bool> TryCompactConversationAsync(ResponsesClient client, string modelId,
        CopilotResponsesSession conversation, string instructions, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (conversation.Items.Count == 0) return false;
        using var content = System.ClientModel.BinaryContent.Create(BinaryData.FromObjectAsJson(new
        {
            model = modelId,
            input = conversation.Items.Select(item => System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
                System.ClientModel.Primitives.ModelReaderWriter.Write(item).ToString())).ToArray(),
            instructions,
        }));
        var result = await client.CompactResponseAsync(content, "application/json",
            new System.ClientModel.Primitives.RequestOptions { CancellationToken = cancellationToken }).ConfigureAwait(false);
        using var document = System.Text.Json.JsonDocument.Parse(result.GetRawResponse().Content);
        var output = document.RootElement.GetProperty("output").EnumerateArray()
            .Select(item => System.ClientModel.Primitives.ModelReaderWriter.Read<ResponseItem>(BinaryData.FromString(item.GetRawText()))
                ?? throw new System.IO.InvalidDataException("Compact 返回了空的原生 Item。"))
            .ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        conversation.ApplyCompaction(output);
        return true;
    }

    internal static ResponseContentPart CreateInputPart(AIContent content) => content switch
    {
        TextContent text => ResponseContentPart.CreateInputTextPart(text.Text),
        DataContent image when image.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) =>
            ResponseContentPart.CreateInputImagePart(image.Uri),
        UriContent image when image.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) =>
            ResponseContentPart.CreateInputImagePart(image.Uri),
        _ => throw new NotSupportedException($"Responses 编程输入不支持 {content.GetType().Name}。"),
    };
}
