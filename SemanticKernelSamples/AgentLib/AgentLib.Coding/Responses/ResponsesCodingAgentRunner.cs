using AgentLib.Model;
using AgentLib.Tools;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace AgentLib.Coding;

internal sealed class ResponsesCodingAgentRunner
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<ResponseItem> _pendingMessages = new();

    internal Task InjectMessageAsync(IReadOnlyList<AIContent> contents, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationToken.ThrowIfCancellationRequested();
        _pendingMessages.Enqueue(ResponseItem.CreateUserMessageItem(contents.Select(ResponsesCodingAgent.CreateInputPart)));
        return Task.CompletedTask;
    }

    private bool AppendPendingMessages()
    {
        bool appended = false;
        while (_pendingMessages.TryDequeue(out ResponseItem? item))
        {
            Conversation.AppendItem(item);
            appended = true;
        }
        return appended;
    }

    public required IManualSendMessageContext MessageContext { get; init; }

    public required IReadOnlyList<AIContent> Contents { get; init; }

    public required CopilotResponsesSession Conversation { get; init; }

    public required ResponseItem Input { get; init; }

    public required string Instructions { get; init; }

    public required ResponsesClient Client { get; init; }

    public required CodingRunWorkspaceContext Workspace { get; init; }

    public required CancellationToken CancellationToken { get; init; }

    public ReasoningEffort? ReasoningEffort { get; init; }

    public bool EnableAutomaticCompression { get; init; }

    internal async Task<string?> RunAsync()
    {
        var functions = Workspace.Tools.OfType<AIFunction>()
            .ToDictionary(function => function.Name, StringComparer.Ordinal);
        await Task.Yield();
        IDisposable? chatting = null;
        try
        {
            await DispatchAsync(() =>
            {
                InitializeUserMessage();
                chatting = MessageContext.StartChatting();
            }).ConfigureAwait(false);
            await MessageContext.AppendMessagesToSessionAsync().ConfigureAwait(false);
            Conversation.AppendItem(Input);

            while (true)
            {
                CancellationToken.ThrowIfCancellationRequested();
                ResponseResult response = await ReceiveResponseAsync(CreateRequest(functions.Values)).ConfigureAwait(false);
                Conversation.AppendResponse(response);
                if (response.Status != ResponseStatus.Completed)
                {
                    throw new InvalidOperationException(response.Error?.Message ?? $"Responses 未完成：{response.Status}");
                }

                var calls = response.OutputItems.OfType<FunctionCallResponseItem>().ToArray();
                if (calls.Length == 0)
                {
                    if (AppendPendingMessages()) continue;
                    return response.GetOutputText();
                }
                await ExecuteToolsAsync(calls, functions).ConfigureAwait(false);
                if (EnableAutomaticCompression && _pendingMessages.IsEmpty && ShouldCompact(response))
                {
                    await ResponsesCodingAgent.TryCompactConversationAsync(Client,
                        MessageContext.LanguageModel.ModelDefinition.ModelId, Conversation,
                        Instructions, CancellationToken).ConfigureAwait(false);
                }
                AppendPendingMessages();
            }
        }
        finally
        {
            await DispatchAsync(() =>
            {
                if (MessageContext.AssistantChatMessage.Content == CopilotChatMessage.PlaceholderContent)
                {
                    MessageContext.AssistantChatMessage.ClearMessageItems();
                }
                chatting?.Dispose();
            }).ConfigureAwait(false);
        }
    }

    private bool ShouldCompact(ResponseResult response)
    {
        long outputCount = response.Usage?.OutputTokenCount > 0
            ? response.Usage.OutputTokenCount
            : response.OutputItems.Sum(EstimateItemTokenCount);
        long toolResults = Conversation.Items.Skip(Conversation.Items.Count - response.OutputItems.OfType<FunctionCallResponseItem>().Count())
            .Sum(EstimateItemTokenCount);
        long contextCount = response.Usage?.TotalTokenCount > 0
            ? response.Usage.TotalTokenCount + toolResults
            : Conversation.Items.Sum(EstimateItemTokenCount);
        return contextCount >= CodingCompressionThresholds.Forced
            || (contextCount >= CodingCompressionThresholds.Conditional
                && outputCount >= CodingCompressionThresholds.MinimumAssistantOutput);
    }

    private static long EstimateItemTokenCount(ResponseItem item)
    {
        // 与现有 Chat 压缩一致：缺少真实用量时按内容字符数估算，不将估算值写入用量统计。
        using var document = System.Text.Json.JsonDocument.Parse(System.ClientModel.Primitives.ModelReaderWriter.Write(item));
        return CountContent(document.RootElement);

        static long CountContent(System.Text.Json.JsonElement element)
        {
            long count = 0;
            if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        if (property.Name is "text" or "output" or "arguments" or "name" or "encrypted_content")
                            count += property.Value.GetString()?.Length ?? 0;
                    }
                    else count += CountContent(property.Value);
                }
            }
            else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
                foreach (var value in element.EnumerateArray()) count += CountContent(value);
            return count;
        }
    }

    private void InitializeUserMessage()
    {
        CopilotChatMessage message = MessageContext.UserChatMessage;
        message.ClearMessageItems();
        foreach (AIContent content in Contents)
        {
            if (content is TextContent text)
            {
                message.AppendText(text.Text);
            }
            else if (content is DataContent image)
            {
                message.MessageItems.Add(new CopilotChatImageItem(BinaryData.FromBytes(image.Data), image.MediaType));
            }
        }
    }

    private CreateResponseOptions CreateRequest(IEnumerable<AIFunction> functions)
    {
        var request = new CreateResponseOptions
        {
            Model = MessageContext.LanguageModel.ModelDefinition.ModelId,
            Instructions = Instructions,
            StreamingEnabled = true,
            StoredOutputEnabled = true,
            ReasoningOptions = ReasoningEffort is { } effort
                ? new ResponseReasoningOptions { ReasoningEffortLevel = new ResponseReasoningEffortLevel(effort.ToString().ToLowerInvariant()) }
                : null,
        };
        foreach (ResponseItem item in Conversation.Items)
        {
            request.InputItems.Add(item);
        }
        foreach (AIFunction function in functions)
        {
            request.Tools.Add(ResponsesToolHelper.CreateTool(function));
        }
        request.Tools.Add(ResponseTool.CreateWebSearchTool());
        return request;
    }

    private async Task<ResponseResult> ReceiveResponseAsync(CreateResponseOptions request)
    {
        ResponseResult? response = null;
        var receivedItems = new SortedDictionary<int, ResponseItem>();
        ResponseResult? startedResponse = null;
        try
        {
            await foreach (StreamingResponseUpdate update in Client
                .CreateResponseStreamingAsync(request, CancellationToken).ConfigureAwait(false))
            {
                await DispatchAsync(() => MessageContext.AssistantChatMessage.ResponseInfo
                    .AppendResponseUpdate(update)).ConfigureAwait(false);
                switch (update)
                {
                    case StreamingResponseCreatedUpdate created:
                        startedResponse = created.Response;
                        break;
                    case StreamingResponseOutputItemDoneUpdate done:
                        receivedItems[done.OutputIndex] = done.Item;
                        break;
                    case StreamingResponseCompletedUpdate completed:
                        response = completed.Response;
                        break;
                    case StreamingResponseIncompleteUpdate incomplete:
                        response = incomplete.Response;
                        break;
                    case StreamingResponseFailedUpdate failed:
                        response = failed.Response;
                        break;
                    case StreamingResponseErrorUpdate error:
                        throw new InvalidOperationException(error.Message);
                }
            }
        }
        catch (InvalidOperationException exception) when (
            exception.Message == "The requested operation requires an element of type 'Array', but the target element has type 'Null'."
            && exception.StackTrace?.Contains("StreamingResponseCompletedUpdate.DeserializeStreamingResponseCompletedUpdate", StringComparison.Ordinal) == true
            && exception.StackTrace.Contains("InternalItemContentOutputText.DeserializeInternalItemContentOutputText", StringComparison.Ordinal))
        {
            CancellationToken.ThrowIfCancellationRequested();
            System.Diagnostics.Trace.TraceWarning($"Responses 已知终态解析异常，按完成处理；终态用量可能缺失。{exception}");
            response = new ResponseResult { Id = startedResponse?.Id, Status = ResponseStatus.Completed };
            foreach (ResponseItem item in receivedItems.Values)
            {
                response.OutputItems.Add(item);
            }
            await DispatchAsync(() => MessageContext.AssistantChatMessage.ResponseInfo.AppendResponse(response)).ConfigureAwait(false);
        }
        return response ?? throw new InvalidOperationException("Responses 流未返回终态响应。");
    }

    private async Task ExecuteToolsAsync(IReadOnlyList<FunctionCallResponseItem> calls,
        IReadOnlyDictionary<string, AIFunction> functions)
    {
        foreach (FunctionCallResponseItem call in calls)
        {
            CancellationToken.ThrowIfCancellationRequested();
            FunctionCallOutputResponseItem output = await ResponsesToolHelper
                .InvokeAsync(functions[call.FunctionName], call, CancellationToken).ConfigureAwait(false);
            Conversation.AppendItem(output);
            await DispatchAsync(() => MessageContext.AssistantChatMessage.ResponseInfo
                .AppendToolResult(output)).ConfigureAwait(false);
        }
    }

    private Task DispatchAsync(Action action)
    {
        if (MessageContext.MainThreadDispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            return dispatcher.InvokeAsync(() =>
            {
                action();
                return Task.CompletedTask;
            });
        }
        action();
        return Task.CompletedTask;
    }
}
