using AgentLib.Model;
using AgentLib.Reducers;
using AgentLib.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001

namespace AgentLib.Coding;

/// <summary>
/// 使用固定编程工作流和工作区工具运行代码任务。
/// </summary>
public sealed class CodingAgent : IAsyncDisposable
{
    private readonly string _languageServerCommand;
    private readonly IReadOnlyList<ICodingWorkspaceToolSource> _additionalToolSources;
    private readonly SemaphoreSlim _workspaceCacheGate = new(1, 1);
    private readonly string? _copilotInstructionsPath;
    private CodingWorkspaceCache? _workspaceCache;
    private readonly object _disposeSync = new();
    private readonly CancellationTokenSource _disposeCancellationTokenSource = new();
    private Task? _disposeTask;
    private int _isDisposed;

    /// <summary>
    /// 创建独占其工作区工具资源的编程代理。
    /// </summary>
    /// <param name="options">代理创建选项。</param>
    public CodingAgent(CodingAgentOptions? options = null)
    {
        options ??= new CodingAgentOptions();
        if (string.IsNullOrWhiteSpace(options.LanguageServerCommand))
        {
            throw new ArgumentException("Language Server 启动命令不能为空。", nameof(options));
        }

        ArgumentNullException.ThrowIfNull(options.AdditionalToolSources);
        ICodingWorkspaceToolSource[] additionalToolSources = [.. options.AdditionalToolSources];
        if (additionalToolSources.Any(static source => source is null))
        {
            throw new ArgumentException("附加工作区工具源不能包含 null。", nameof(options));
        }

        _languageServerCommand = options.LanguageServerCommand;
        _additionalToolSources = additionalToolSources;
        _copilotInstructionsPath = options.CopilotInstructionsPath;
    }

    /// <summary>
    /// 获取当前缓存的代码工作区路径。
    /// </summary>
    public string? WorkspacePath => _workspaceCache?.WorkspacePath;

    /// <summary>
    /// 使用纯文本运行一次编程任务。
    /// </summary>
    /// <param name="context">现有手动发送上下文。</param>
    /// <param name="prompt">用户任务文本。</param>
    /// <param name="workspacePath">本次运行期望使用的工作区路径。</param>
    /// <param name="enableAutomaticCompression">是否自动压缩对话历史。</param>
    /// <param name="enableDotNetRun">是否为本次运行提供 <c>dotnet run</c> 工具。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>流式消息和完整生命周期任务。</returns>
    public Task<CodingAgentRunResult> RunAsync
    (
        IManualSendMessageContext context,
        string prompt,
        string? workspacePath,
        bool enableAutomaticCompression = true,
        bool enableDotNetRun = false,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("编程任务文本不能为空。", nameof(prompt));
        }

        return RunAsync
        (
            context,
            [new TextContent(prompt)],
            workspacePath,
            enableAutomaticCompression,
            enableDotNetRun,
            cancellationToken
        );
    }

    /// <summary>
    /// 使用有序多模态内容运行一次编程任务。
    /// </summary>
    /// <param name="context">现有手动发送上下文。</param>
    /// <param name="contents">保持原始顺序的用户输入内容。</param>
    /// <param name="workspacePath">本次运行期望使用的工作区路径。</param>
    /// <param name="enableAutomaticCompression">是否自动压缩对话历史。</param>
    /// <param name="enableDotNetRun">是否为本次运行提供 <c>dotnet run</c> 工具。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>流式消息和完整生命周期任务。</returns>
    public async Task<CodingAgentRunResult> RunAsync
    (
        IManualSendMessageContext context,
        IReadOnlyList<AIContent> contents,
        string? workspacePath,
        bool enableAutomaticCompression = true,
        bool enableDotNetRun = false,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contents);
        AIContent[] runContents = [.. contents];
        if (runContents.Length == 0)
        {
            throw new ArgumentException("编程任务内容不能为空。", nameof(contents));
        }

        CancellationTokenSource? runCancellationTokenSource = null;
        bool ownershipTransferred = false;
        try
        {
            ThrowIfDisposed();
            runCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource
            (
                cancellationToken,
                _disposeCancellationTokenSource.Token
            );
            CancellationToken runCancellationToken = runCancellationTokenSource.Token;
            CodingRunWorkspaceContext workspaceContext = await GetRunWorkspaceContextAsync(
                workspacePath,
                enableDotNetRun,
                runCancellationToken).ConfigureAwait(false);
            ChatClientAgent chatClientAgent = await context.GetChatClientAgentAsync
            (
                options =>
                {
                    options.ChatOptions ??= new ChatOptions();
                    options.ChatOptions.Tools = [.. workspaceContext.Tools];
                    options.AIContextProviders = [];
                    options.EnableMessageInjection = true;
                    options.RequirePerServiceCallChatHistoryPersistence = true;
                    if (enableAutomaticCompression)
                    {
                        var reducer = new CopilotChatManagerToolCallChatReducer(context.ChatClient)
                        {
                            ConditionalCompressionTokenCountThreshold = 200_000,
                            ForcedCompressionTokenCountThreshold = 300_000,
                        };
                        _ = new CompressionToolCallObserver
                        (
                            context.AssistantChatMessage,
                            context.MainThreadDispatcher,
                            reducer
                        );
                        options.ChatHistoryProvider = new InMemoryChatHistoryProvider
                        (
                            new InMemoryChatHistoryProviderOptions
                            {
                                ChatReducer = new ToolCallAwareChatReducer(reducer),
                            }
                        );
                    }
                    else
                    {
                        options.ChatHistoryProvider = null;
                    }
                }, runCancellationToken
            ).ConfigureAwait(false);
            MessageInjectingChatClient messageInjector = chatClientAgent.GetService<MessageInjectingChatClient>()
                                                         ?? throw new InvalidOperationException("编程代理未启用消息注入。");
            AgentSession agentSession = await context.GetAgentSessionAsync(runCancellationToken).ConfigureAwait(false);
            Task<string?> completionTask = RunCoreAsync
            (
                context,
                runContents,
                chatClientAgent,
                agentSession,
                workspaceContext.ToolRegistrationRegistry,
                runCancellationTokenSource
            );
            ownershipTransferred = true;
            return new CodingAgentRunResult
            (
                context.AssistantChatMessage,
                completionTask,
                messageInjector,
                agentSession
            );
        }
        finally
        {
            if (!ownershipTransferred)
            {
                runCancellationTokenSource?.Dispose();
            }
        }
    }

    /// <summary>
    /// 停止当前工作区缓存中的 Roslyn Language Server。
    /// </summary>
    /// <returns>存在活动 Language Server 并已停止时返回 <see langword="true"/>。</returns>
    public Task<bool> StopLanguageServerAsync() =>
        _workspaceCache?.StopLanguageServerAsync() ?? Task.FromResult(false);

    private async Task<CodingRunWorkspaceContext> GetRunWorkspaceContextAsync(
        string? workspacePath,
        bool enableDotNetRun,
        CancellationToken cancellationToken)
    {
        string? normalizedPath = string.IsNullOrWhiteSpace(workspacePath)
            ? null
            : Path.GetFullPath(workspacePath);
        await _workspaceCacheGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            if (AreSameWorkspace(_workspaceCache?.WorkspacePath, normalizedPath))
            {
                return CreateRunWorkspaceContext(_workspaceCache, enableDotNetRun);
            }

            CodingWorkspaceCache? replacement = normalizedPath is null
                ? null
                : CodingWorkspaceCache.Create(
                    normalizedPath,
                    _languageServerCommand);
            CodingWorkspaceCache? previous = _workspaceCache;
            _workspaceCache = replacement;
            if (previous is not null)
            {
                await previous.DisposeAsync().ConfigureAwait(false);
            }

            return CreateRunWorkspaceContext(replacement, enableDotNetRun);
        }
        finally
        {
            _workspaceCacheGate.Release();
        }
    }

    private CodingRunWorkspaceContext CreateRunWorkspaceContext(
        CodingWorkspaceCache? workspaceCache,
        bool enableDotNetRun)
    {
        if (workspaceCache is null)
        {
            return CodingRunWorkspaceContext.Empty;
        }

        IReadOnlyList<ToolRegistration> additionalToolRegistrations = _additionalToolSources
            .SelectMany(source => source.CreateToolRegistrations(workspaceCache.WorkspacePath))
            .ToArray();
        return workspaceCache.CreateRunContext(additionalToolRegistrations, enableDotNetRun);
    }

    /// <summary>
    /// 异步取消活动运行，并在它们完成清理后释放工作区缓存。
    /// </summary>
    public ValueTask DisposeAsync()
    {
        lock (_disposeSync)
        {
            _disposeTask ??= DisposeCoreAsync();
            return new ValueTask(_disposeTask);
        }
    }

    private async Task DisposeCoreAsync()
    {
        Volatile.Write(ref _isDisposed, 1);
        try
        {
            _disposeCancellationTokenSource.Cancel();
            await _workspaceCacheGate.WaitAsync().ConfigureAwait(false);
            try
            {
                CodingWorkspaceCache? cache = _workspaceCache;
                _workspaceCache = null;
                if (cache is not null)
                {
                    await cache.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _workspaceCacheGate.Release();
                _workspaceCacheGate.Dispose();
            }
        }
        finally
        {
            _disposeCancellationTokenSource.Dispose();
        }
    }

    private async Task<string?> RunCoreAsync
    (
        IManualSendMessageContext context,
        IReadOnlyList<AIContent> contents,
        ChatClientAgent chatClientAgent,
        AgentSession agentSession,
        ToolRegistrationRegistry toolRegistrationRegistry,
        CancellationTokenSource runCancellationTokenSource
    )
    {
        bool hasResponseUpdate = false;
        try
        {
            await Task.Yield();
            CancellationToken cancellationToken = runCancellationTokenSource.Token;
            CopilotChatMessage userChatMessage = context.UserChatMessage;
            userChatMessage.ClearMessageItems();
            foreach (AIContent item in contents)
            {
                switch (item)
                {
                    case TextContent textContent:
                        userChatMessage.AppendText(textContent.Text);
                        break;
                    case DataContent dataContent when dataContent.Data is { Length: > 0 }:
                        userChatMessage.MessageItems.Add(CreateDataMessageItem(dataContent));
                        break;
                }
            }

            using IDisposable chatting = context.StartChatting();
            await context.AppendMessagesToSessionAsync();
            await CodingSystemPrompt
                .EnsureSystemPromptInSessionAsync(agentSession, _copilotInstructionsPath, cancellationToken)
                .ConfigureAwait(false);
            ChatMessage[] inputMessages =
            [
                new ChatMessage(ChatRole.User, new List<AIContent>(contents)),
            ];

            await foreach (AgentResponseUpdate update in chatClientAgent.RunWithHistoryCompletionAsync
                           (
                               inputMessages,
                               agentSession,
                               cancellationToken
                           ))
            {
                AppendResponseUpdate(context, update, toolRegistrationRegistry);
                hasResponseUpdate = true;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!hasResponseUpdate)
            {
                await ClearAssistantPlaceholderAsync(context).ConfigureAwait(false);
            }

            string content = context.AssistantChatMessage.Content;
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
        finally
        {
            try
            {
                if (!hasResponseUpdate)
                {
                    await ClearAssistantPlaceholderAsync(context).ConfigureAwait(false);
                }
            }
            finally
            {
                runCancellationTokenSource.Dispose();
            }
        }
    }

    private static void AppendResponseUpdate
    (
        IManualSendMessageContext context,
        AgentResponseUpdate update,
        ToolRegistrationRegistry registry
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(registry);
        if (context.AssistantChatMessage.Content == CopilotChatMessage.PlaceholderContent)
        {
            context.AssistantChatMessage.ClearMessageItems();
        }

        foreach (AIContent content in update.Contents)
        {
            switch (content)
            {
                case TextReasoningContent textReasoningContent when !string.IsNullOrEmpty(textReasoningContent.Text):
                    context.AssistantChatMessage.AppendReasoning(textReasoningContent.Text);
                    break;
                case TextContent textContent when !string.IsNullOrEmpty(textContent.Text):
                    context.AssistantChatMessage.AppendText(textContent.Text);
                    break;
                case FunctionCallContent functionCallContent:
                    context.AssistantChatMessage.AppendFunctionCall
                        (functionCallContent, registry.CreatePresentation(functionCallContent));
                    break;
                case FunctionResultContent functionResultContent:
                    context.AssistantChatMessage.AppendFunctionResult(functionResultContent);
                    break;
            }
        }

        context.AssistantChatMessage.AppendUsageDetails(update.Contents);
    }

    private static ICopilotChatMessageItem CreateDataMessageItem(DataContent dataContent)
    {
        ReadOnlyMemory<byte> data = dataContent.Data;
        string mimeType = dataContent.MediaType ?? string.Empty;
        if (mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return new CopilotChatAudioItem(BinaryData.FromBytes(data), mimeType);
        }

        return new CopilotChatImageItem
        (
            BinaryData.FromBytes(data),
            string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType
        );
    }

    private static Task ClearAssistantPlaceholderAsync(IManualSendMessageContext context)
    {
        if (context.AssistantChatMessage.Content != CopilotChatMessage.PlaceholderContent)
        {
            return Task.CompletedTask;
        }

        context.AssistantChatMessage.ClearMessageItems();
        return Task.CompletedTask;
    }

    private static bool AreSameWorkspace(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals
        (
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
        );
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _isDisposed) != 0)
        {
            throw new ObjectDisposedException(nameof(CodingAgent));
        }
    }
}