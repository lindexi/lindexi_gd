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
    /// 使用有序多模态内容和运行选项执行编程对话。
    /// </summary>
    /// <param name="context">本次编程执行的模型会话和输出上下文。</param>
    /// <param name="contents">保持原始顺序的用户输入内容。</param>
    /// <param name="workspacePath">本次运行期望使用的工作区路径。</param>
    /// <param name="options">运行选项；未指定时使用默认选项。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>流式消息和完整生命周期任务。</returns>
    public async Task<ICodingAgentRunResult> RunAsync(
        CodingRunContext context,
        IReadOnlyList<AIContent> contents,
        string? workspacePath,
        CodingChatRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contents);
        AIContent[] runContents = [.. contents];
        if (runContents.Length == 0)
        {
            throw new ArgumentException("编程任务内容不能为空。", nameof(contents));
        }

        ThrowIfDisposed();
        CodingChatRunOptions runOptions = options ?? CodingChatRunOptions.Default;
        CodingRunWorkspaceContext workspaceContext = await GetRunWorkspaceContextAsync(
            workspacePath, runOptions.EnableDotNetRun, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<string> prompts = await CodingPromptProvider.BuildAsync(
            _copilotInstructionsPath, cancellationToken).ConfigureAwait(false);
        var delegation = new CodingSubAgentTools(this, context, workspaceContext, prompts, runOptions);
        return await RunAsync(context, runContents, workspaceContext.WithTool(delegation.CreateRegistration()),
            prompts, runOptions, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<ICodingAgentRunResult> RunAsync(
        CodingRunContext context,
        IReadOnlyList<AIContent> contents,
        CodingRunWorkspaceContext workspaceContext,
        IReadOnlyList<string> prompts,
        CodingChatRunOptions runOptions,
        CancellationToken cancellationToken)
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
            runCancellationToken.ThrowIfCancellationRequested();
            var options = new ChatClientAgentOptions();
            {
                    options.ChatOptions ??= new ChatOptions();
                    options.ChatOptions.Tools = [.. workspaceContext.Tools];
                    options.AIContextProviders = [];
                    options.EnableMessageInjection = true;
                    options.RequirePerServiceCallChatHistoryPersistence = true;
                    if (runOptions.ReasoningEffort is { } effort)
                    {
                        options.ChatOptions.Reasoning = new ReasoningOptions { Effort = effort };
                    }
                    if (runOptions.EnableAutomaticCompression)
                    {
                        var reducer = new CopilotChatManagerToolCallChatReducer(context.ChatClient)
                        {
                            ConditionalCompressionTokenCountThreshold = CodingCompressionThresholds.Conditional,
                            ForcedCompressionTokenCountThreshold = CodingCompressionThresholds.Forced,
                        };
                        var compressionObserver = new CompressionToolCallObserver(context, reducer);
                        options.ChatHistoryProvider = new InMemoryChatHistoryProvider
                        (
                            new InMemoryChatHistoryProviderOptions
                            {
                                ChatReducer = new ToolCallAwareChatReducer(compressionObserver),
                            }
                        );
                    }
                    else
                    {
                        options.ChatHistoryProvider = null;
                    }
            }
            ChatClientAgent chatClientAgent = context.ChatClient.AsAIAgent(options);
            MessageInjectingChatClient messageInjector = chatClientAgent.GetService<MessageInjectingChatClient>()
                                                         ?? throw new InvalidOperationException("编程代理未启用消息注入。");
            AgentSession agentSession = context.AgentSession
                ?? await chatClientAgent.CreateSessionAsync(runCancellationToken).ConfigureAwait(false);
            context.AgentSession = agentSession;
            Task<string?> completionTask = RunCoreAsync
            (
                context,
                runContents,
                chatClientAgent,
                agentSession,
                workspaceContext.ToolRegistrationRegistry,
                prompts,
                runCancellationTokenSource
            );
            ownershipTransferred = true;
            return new ChatClientCodingAgentRunResult
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

    internal async Task<CodingRunWorkspaceContext> GetRunWorkspaceContextAsync
    (
        string? workspacePath,
        bool enableDotNetRun,
        CancellationToken cancellationToken
    )
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
                : CodingWorkspaceCache.Create
                (
                    normalizedPath,
                    _languageServerCommand
                );
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

    private CodingRunWorkspaceContext CreateRunWorkspaceContext
    (
        CodingWorkspaceCache? workspaceCache,
        bool enableDotNetRun
    )
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
        CodingRunContext context,
        IReadOnlyList<AIContent> contents,
        ChatClientAgent chatClientAgent,
        AgentSession agentSession,
        ToolRegistrationRegistry toolRegistrationRegistry,
        IReadOnlyList<string> prompts,
        CancellationTokenSource runCancellationTokenSource
    )
    {
        bool hasResponseUpdate = false;
        try
        {
            await Task.Yield();
            CancellationToken cancellationToken = runCancellationTokenSource.Token;

            await CodingSystemPrompt
                .EnsureSystemPromptInSessionAsync(agentSession, prompts, cancellationToken)
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
                await context.InvokeOutputAsync(() =>
                    AppendResponseUpdate(context, update, toolRegistrationRegistry)).ConfigureAwait(false);
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
        CodingRunContext context,
        AgentResponseUpdate update,
        ToolRegistrationRegistry registry
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(registry);
        context.Output.ClearPlaceholder();

        foreach (AIContent content in update.Contents)
        {
            switch (content)
            {
                case TextReasoningContent textReasoningContent when !string.IsNullOrEmpty(textReasoningContent.Text):
                    context.Output.AppendReasoning(textReasoningContent.Text);
                    break;
                case TextContent textContent when !string.IsNullOrEmpty(textContent.Text):
                    context.Output.AppendText(textContent.Text);
                    break;
                case FunctionCallContent functionCallContent:
                    context.Output.AppendFunctionCall
                        (functionCallContent, registry.CreatePresentation(functionCallContent));
                    break;
                case FunctionResultContent functionResultContent:
                    context.Output.AppendFunctionResult(functionResultContent);
                    break;
            }
        }

        context.Output.AppendUsageDetails(update.Contents);
    }

    private static Task ClearAssistantPlaceholderAsync(CodingRunContext context) =>
        context.InvokeOutputAsync(context.Output.ClearPlaceholder);

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