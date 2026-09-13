using System.ClientModel;
using System.Text.Json;
using AgentLib.Model;
using AgentLib.Tools;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace AgentLib.Coding.Responses;

/// <summary>
/// 使用 OpenAI Responses API 和本地编程工具运行代码任务。
/// </summary>
public sealed class ResponsesCodingAgent : IAsyncDisposable
{
    private readonly Func<string, ResponsesClient> _createClient;
    private readonly ResponsesCodingAgentOptions _options;
    private readonly SemaphoreSlim _workspaceGate = new(1, 1);
    private CodingWorkspaceCache? _workspaceCache;
    private ResponsesClient? _activeClient;
    private string? _activeResponseId;

    /// <summary>
    /// 创建原生 Responses 编程代理。
    /// </summary>
    public ResponsesCodingAgent(
        Func<string, ResponsesClient> createClient,
        ResponsesCodingAgentOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(createClient);
        _createClient = createClient;
        _options = options ?? new ResponsesCodingAgentOptions();
    }

    /// <summary>
    /// 运行一次编程任务。
    /// </summary>
    public async Task<ResponsesCodingRunResult> RunAsync(
        CopilotChatSession session,
        IReadOnlyList<AIContent> contents,
        string model,
        string? workspacePath,
        ResponsesCodingRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        options ??= new ResponsesCodingRunOptions();

        CodingRunWorkspaceContext workspace = await GetWorkspaceAsync(
            workspacePath,
            options.EnableDotNetRun,
            cancellationToken).ConfigureAwait(false);
        var registry = new ResponsesToolRegistry(workspace.ToolRegistrations);
        CopilotChatMessage userMessage = new(ChatRole.User, contents);
        CopilotChatMessage assistantMessage = new(ChatRole.Assistant, CopilotChatMessage.PlaceholderContent);
        await AddMessageAsync(session, userMessage).ConfigureAwait(false);
        await AddMessageAsync(session, assistantMessage).ConfigureAwait(false);
        session.WorkspacePath = workspace.WorkspacePath;

        ResponsesClient client = _createClient(model);
        Task<string?> completionTask = RunCoreAsync(
            client,
            session,
            assistantMessage,
            contents,
            model,
            registry,
            options,
            cancellationToken);
        return new ResponsesCodingRunResult(assistantMessage, completionTask);
    }

    /// <summary>
    /// 取消当前服务端 Response。
    /// </summary>
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        string? responseId = _activeResponseId;
        if (!string.IsNullOrWhiteSpace(responseId))
        {
            await _activeClient!.CancelResponseAsync(responseId, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 使用 Responses API 压缩当前服务端对话上下文。
    /// </summary>
    public async Task CompactAsync(
        CopilotChatSession session,
        string model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        if (string.IsNullOrWhiteSpace(session.ResponsesLastResponseId))
        {
            return;
        }

        ResponsesClient client = _createClient(model);
        using BinaryContent content = BinaryContent.CreateJson(new
        {
            model,
            previous_response_id = session.ResponsesLastResponseId,
        });
        ClientResult result = await client.CompactResponseAsync(
            content,
            "application/json",
            new System.ClientModel.Primitives.RequestOptions
            {
                CancellationToken = cancellationToken,
            }).ConfigureAwait(false);
        BinaryData responseContent = await result.GetRawResponse()
            .BufferContentAsync(cancellationToken)
            .ConfigureAwait(false);
        using JsonDocument document = JsonDocument.Parse(responseContent);
        if (document.RootElement.TryGetProperty("id", out JsonElement idElement))
        {
            session.ResponsesLastResponseId = idElement.GetString();
        }
    }

    /// <summary>
    /// 停止当前工作区的 Roslyn Language Server。
    /// </summary>
    public Task<bool> StopLanguageServerAsync() =>
        _workspaceCache?.StopLanguageServerAsync() ?? Task.FromResult(false);

    private async Task<string?> RunCoreAsync(
        ResponsesClient client,
        CopilotChatSession session,
        CopilotChatMessage assistantMessage,
        IReadOnlyList<AIContent> contents,
        string model,
        ResponsesToolRegistry registry,
        ResponsesCodingRunOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<ResponseItem> inputItems = CreateInputItems(session, contents);
            string? previousResponseId = session.ResponsesLastResponseId;
            string instructions = await CodingSystemPrompt.BuildInstructionsAsync(
                _options.CopilotInstructionsPath,
                cancellationToken).ConfigureAwait(false);

            while (true)
            {
                CreateResponseOptions request = CreateRequest(
                    model,
                    inputItems,
                    previousResponseId,
                    instructions,
                    registry.Tools,
                    options.ReasoningEffort);
                ResponseResult response = await StreamResponseAsync(
                    client,
                    request,
                    session,
                    assistantMessage,
                    registry,
                    cancellationToken).ConfigureAwait(false);

                previousResponseId = response.Id;
                session.ResponsesLastResponseId = response.Id;
                session.ResponsesMessageCount = session.ChatMessages.Count;
                AppendUsage(assistantMessage, response);

                FunctionCallResponseItem[] calls = response.OutputItems
                    .OfType<FunctionCallResponseItem>()
                    .ToArray();
                if (calls.Length == 0)
                {
                    if (response.Status == ResponseStatus.Failed)
                    {
                        throw new InvalidOperationException(response.Error?.Message ?? "Responses API 请求失败。");
                    }

                    if (response.Status == ResponseStatus.Incomplete)
                    {
                        throw new InvalidOperationException($"Responses API 响应未完成：{response.IncompleteStatusDetails?.Reason}");
                    }

                    break;
                }

                var outputs = new List<ResponseItem>(calls.Length);
                foreach (FunctionCallResponseItem call in calls)
                {
                    FunctionCallContent displayCall = CreateFunctionCallContent(call);
                    await UpdateMessageAsync(() =>
                    {
                        EnsurePlaceholderCleared(assistantMessage);
                        assistantMessage.AppendFunctionCall(displayCall, registry.CreatePresentation(displayCall));
                    }).ConfigureAwait(false);
                    object? result = await registry.InvokeAsync(
                        call.FunctionName,
                        call.FunctionArguments,
                        cancellationToken).ConfigureAwait(false);
                    await UpdateMessageAsync(() =>
                        assistantMessage.AppendFunctionResult(new FunctionResultContent(call.CallId, result))).ConfigureAwait(false);
                    outputs.Add(ResponseItem.CreateFunctionCallOutputItem(
                        call.CallId,
                        SerializeToolResult(result)));
                }

                inputItems = outputs;
                instructions = string.Empty;
            }

            string text = assistantMessage.Content;
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        finally
        {
            _activeClient = null;
            _activeResponseId = null;
            session.ResponsesActiveResponseId = null;
            await UpdateMessageAsync(() => EnsurePlaceholderCleared(assistantMessage)).ConfigureAwait(false);
        }
    }

    private async Task<ResponseResult> StreamResponseAsync(
        ResponsesClient client,
        CreateResponseOptions request,
        CopilotChatSession session,
        CopilotChatMessage assistantMessage,
        ResponsesToolRegistry registry,
        CancellationToken cancellationToken)
    {
        ResponseResult? completedResponse = null;
        await foreach (StreamingResponseUpdate update in client.CreateResponseStreamingAsync(request, cancellationToken))
        {
            switch (update)
            {
                case StreamingResponseCreatedUpdate created:
                    _activeClient = client;
                                       _activeResponseId = created.Response.Id;
                    session.ResponsesActiveResponseId = created.Response.Id;
                    break;
                case StreamingResponseOutputTextDeltaUpdate text:
                    await UpdateMessageAsync(() =>
                    {
                        EnsurePlaceholderCleared(assistantMessage);
                        assistantMessage.AppendText(text.Delta);
                    }).ConfigureAwait(false);
                    break;
                case StreamingResponseReasoningTextDeltaUpdate reasoning:
                    await UpdateMessageAsync(() =>
                    {
                        EnsurePlaceholderCleared(assistantMessage);
                        assistantMessage.AppendReasoning(reasoning.Delta);
                    }).ConfigureAwait(false);
                    break;
                case StreamingResponseReasoningSummaryTextDeltaUpdate summary:
                    await UpdateMessageAsync(() =>
                    {
                        EnsurePlaceholderCleared(assistantMessage);
                        assistantMessage.AppendReasoning(summary.Delta);
                    }).ConfigureAwait(false);
                    break;
                case StreamingResponseRefusalDeltaUpdate refusal:
                    await UpdateMessageAsync(() =>
                    {
                        EnsurePlaceholderCleared(assistantMessage);
                        assistantMessage.AppendText(refusal.Delta);
                    }).ConfigureAwait(false);
                    break;
                case StreamingResponseErrorUpdate error:
                    throw new InvalidOperationException(error.Message);
                case StreamingResponseOutputItemDoneUpdate itemDone when itemDone.Item is FunctionCallResponseItem call:
                    FunctionCallContent displayCall = CreateFunctionCallContent(call);
                    await UpdateMessageAsync(() =>
                    {
                        EnsurePlaceholderCleared(assistantMessage);
                        assistantMessage.AppendFunctionCall(displayCall, registry.CreatePresentation(displayCall));
                    }).ConfigureAwait(false);
                    break;
                case StreamingResponseCompletedUpdate completed:
                    completedResponse = completed.Response;
                    break;
                case StreamingResponseIncompleteUpdate incomplete:
                    completedResponse = incomplete.Response;
                    break;
                case StreamingResponseFailedUpdate failed:
                    completedResponse = failed.Response;
                    break;
            }
        }

        return completedResponse ?? throw new InvalidOperationException("Responses API 流结束时没有返回最终响应。");
    }

    private static CreateResponseOptions CreateRequest(
        string model,
        IReadOnlyList<ResponseItem> inputItems,
        string? previousResponseId,
        string instructions,
        IReadOnlyList<ResponseTool> tools,
        ReasoningEffort? reasoningEffort)
    {
        var request = new CreateResponseOptions(model, inputItems)
        {
            PreviousResponseId = previousResponseId,
            StoredOutputEnabled = true,
            BackgroundModeEnabled = true,
            ParallelToolCallsEnabled = false,
            ToolChoice = tools.Count == 0 ? ResponseToolChoice.CreateNoneChoice() : ResponseToolChoice.CreateAutoChoice(),
            Instructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions,
        };
        foreach (ResponseTool tool in tools)
        {
            request.Tools.Add(tool);
        }

        if (reasoningEffort is not null)
        {
            request.ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = reasoningEffort.Value switch
                {
                    ReasoningEffort.None => ResponseReasoningEffortLevel.None,
                    ReasoningEffort.Low => ResponseReasoningEffortLevel.Low,
                    ReasoningEffort.Medium => ResponseReasoningEffortLevel.Medium,
                    ReasoningEffort.High => ResponseReasoningEffortLevel.High,
                    ReasoningEffort.ExtraHigh => new ResponseReasoningEffortLevel("xhigh"),
                    _ => null,
                },
                ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Auto,
            };
            request.IncludedProperties.Add(IncludedResponseProperty.ReasoningEncryptedContent);
        }

        return request;
    }

    private static IReadOnlyList<ResponseItem> CreateInputItems(
        CopilotChatSession session,
        IReadOnlyList<AIContent> contents)
    {
        var items = new List<ResponseItem>();
        int unsynchronizedCount = Math.Max(0, session.ChatMessages.Count - 2 - session.ResponsesMessageCount);
        foreach (CopilotChatMessage message in session.ChatMessages.Skip(session.ResponsesMessageCount).Take(unsynchronizedCount))
        {
            ResponseItem? item = CreateInputItem(message);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        var parts = new List<ResponseContentPart>();
        foreach (AIContent content in contents)
        {
            switch (content)
            {
                case TextContent text when !string.IsNullOrWhiteSpace(text.Text):
                    parts.Add(ResponseContentPart.CreateInputTextPart(text.Text));
                    break;
                case DataContent data when data.Data.Length > 0 && data.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true:
                    parts.Add(ResponseContentPart.CreateInputImagePart(BinaryData.FromBytes(data.Data)));
                    break;
                case DataContent data when data.Data.Length > 0:
                    parts.Add(ResponseContentPart.CreateInputFilePart(
                        BinaryData.FromBytes(data.Data),
                        data.MediaType ?? "application/octet-stream",
                        data.Name ?? "attachment"));
                    break;
            }
        }

        items.Add(ResponseItem.CreateUserMessageItem(parts));
        return items;
    }

    private static ResponseItem? CreateInputItem(CopilotChatMessage message)
    {
        var parts = new List<ResponseContentPart>();
        foreach (ICopilotChatMessageItem item in message.MessageItems)
        {
            switch (item)
            {
                case CopilotChatTextItem text when !string.IsNullOrWhiteSpace(text.Text):
                    parts.Add(message.Role == ChatRole.Assistant
                        ? ResponseContentPart.CreateOutputTextPart(text.Text, [])
                        : ResponseContentPart.CreateInputTextPart(text.Text));
                    break;
                case CopilotChatImageItem image when message.Role == ChatRole.User:
                    parts.Add(ResponseContentPart.CreateInputImagePart(image.Data));
                    break;
                case CopilotChatToolItem tool when message.Role == ChatRole.Assistant:
                    parts.Add(ResponseContentPart.CreateOutputTextPart(
                        $"工具 {tool.ToolName} 输入：{tool.InputText}\n输出：{tool.OutputText}",
                        []));
                    break;
            }
        }

        if (parts.Count == 0)
        {
            return null;
        }

        return message.Role switch
        {
            var role when role == ChatRole.Assistant => ResponseItem.CreateAssistantMessageItem(parts),
            var role when role == ChatRole.System => ResponseItem.CreateSystemMessageItem(parts),
            _ => ResponseItem.CreateUserMessageItem(parts),
        };
    }

    private static FunctionCallContent CreateFunctionCallContent(FunctionCallResponseItem call)
    {
        Dictionary<string, object?> arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(
            call.FunctionArguments.ToString()) ?? [];
        return new FunctionCallContent(call.CallId, call.FunctionName, arguments!);
    }

    private static string SerializeToolResult(object? result)
    {
        return result switch
        {
            null => "null",
            string text => text,
            _ => JsonSerializer.Serialize(result),
        };
    }

    private static void AppendUsage(CopilotChatMessage message, ResponseResult response)
    {
        if (response.Usage is null)
        {
            return;
        }

        message.AppendUsageDetails(new UsageDetails
        {
            InputTokenCount = response.Usage.InputTokenCount,
            OutputTokenCount = response.Usage.OutputTokenCount,
            TotalTokenCount = response.Usage.TotalTokenCount,
            ReasoningTokenCount = response.Usage.OutputTokenDetails?.ReasoningTokenCount,
        });
    }

    private static void EnsurePlaceholderCleared(CopilotChatMessage message)
    {
        if (message.Content == CopilotChatMessage.PlaceholderContent)
        {
            message.ClearMessageItems();
        }
    }

    private Task AddMessageAsync(CopilotChatSession session, CopilotChatMessage message)
    {
        if (_options.MainThreadDispatcher is null || _options.MainThreadDispatcher.CheckAccess())
        {
            return session.AddMessageAsync(message);
        }

        return _options.MainThreadDispatcher.InvokeAsync(() => session.AddMessageAsync(message));
    }

    private Task UpdateMessageAsync(Action action)
    {
        if (_options.MainThreadDispatcher is null || _options.MainThreadDispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return _options.MainThreadDispatcher.InvokeAsync(() =>
        {
            action();
            return Task.CompletedTask;
        });
    }

    private async Task<CodingRunWorkspaceContext> GetWorkspaceAsync(
        string? workspacePath,
        bool enableDotNetRun,
        CancellationToken cancellationToken)
    {
        string? normalizedPath = string.IsNullOrWhiteSpace(workspacePath) ? null : Path.GetFullPath(workspacePath);
        await _workspaceGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (string.Equals(_workspaceCache?.WorkspacePath, normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                return CreateRunContext(_workspaceCache, enableDotNetRun);
            }

            CodingWorkspaceCache? replacement = normalizedPath is null
                ? null
                : CodingWorkspaceCache.Create(normalizedPath, _options.LanguageServerCommand);
            CodingWorkspaceCache? previous = _workspaceCache;
            _workspaceCache = replacement;
            if (previous is not null)
            {
                await previous.DisposeAsync().ConfigureAwait(false);
            }

            return CreateRunContext(replacement, enableDotNetRun);
        }
        finally
        {
            _workspaceGate.Release();
        }
    }

    private CodingRunWorkspaceContext CreateRunContext(CodingWorkspaceCache? cache, bool enableDotNetRun)
    {
        if (cache is null)
        {
            return CodingRunWorkspaceContext.Empty;
        }

        ToolRegistration[] additional = _options.AdditionalToolSources
            .SelectMany(source => source.CreateToolRegistrations(cache.WorkspacePath))
            .ToArray();
        return cache.CreateRunContext(additional, enableDotNetRun);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _workspaceGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_workspaceCache is not null)
            {
                await _workspaceCache.DisposeAsync().ConfigureAwait(false);
                _workspaceCache = null;
            }
        }
        finally
        {
            _workspaceGate.Release();
            _workspaceGate.Dispose();
        }
    }
}
