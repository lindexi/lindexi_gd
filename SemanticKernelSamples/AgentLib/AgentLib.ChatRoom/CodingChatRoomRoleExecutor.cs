using AgentLib.ChatRoom.Model;
using AgentLib.Coding;
using AgentLib.Model;
using Microsoft.Extensions.AI;

namespace AgentLib.ChatRoom;

internal sealed class CodingChatRoomRoleExecutor : IChatRoomRoleExecutor
{
    private readonly CodingAgent _codingAgent;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly object _disposeSync = new();
    private string? _workspacePath;
    private Task? _disposeTask;
    private int _isDisposed;

    internal CodingChatRoomRoleExecutor(CodingAgent codingAgent)
    {
        ArgumentNullException.ThrowIfNull(codingAgent);
        _codingAgent = codingAgent;
    }

    public ChatRoomRoleExecutionKind ExecutionKind => ChatRoomRoleExecutionKind.Coding;

    public async Task<ChatRoomRoleExecutionResult> RunAsync
    (
        ChatRoomRoleExecutionContext context,
        IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contents);
        ThrowIfDisposed();

        IManualSendMessageContext manualContext = await context.ChatManager
            .CreateManualSendMessageContextAsync(cancellationToken)
            .ConfigureAwait(false);

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            ICodingAgentRunResult runResult = await _codingAgent.RunAsync
            (
                manualContext,
                contents,
                _workspacePath,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            return new ChatRoomRoleExecutionResult
            (
                runResult.AssistantChatMessage,
                CompleteAsync(runResult.CompletionTask, cancellationToken)
            );
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public Task SetWorkspacePathAsync
    (
        CopilotChatManager chatManager,
        string? workspacePath,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        string? normalizedPath = string.IsNullOrWhiteSpace(workspacePath)
            ? null
            : Path.GetFullPath(workspacePath);
        if (normalizedPath is not null && !Directory.Exists(normalizedPath))
        {
            throw new DirectoryNotFoundException($"指定的工作路径不存在：{normalizedPath}");
        }

        _workspacePath = normalizedPath;
        chatManager.WorkspacePath = normalizedPath;
        return Task.CompletedTask;
    }

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
        await _lifecycleLock.WaitAsync().ConfigureAwait(false);
        _lifecycleLock.Release();
        await _codingAgent.DisposeAsync().ConfigureAwait(false);
    }

    private async Task<ChatRoomRoleExecutionCompletion> CompleteAsync
    (
        Task<string?> completionTask,
        CancellationToken cancellationToken
    )
    {
        try
        {
            string? content = await completionTask.ConfigureAwait(false);
            return new ChatRoomRoleExecutionCompletion(content, WasCanceled: false);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested || Volatile.Read(ref _isDisposed) != 0)
        {
            return new ChatRoomRoleExecutionCompletion(null, WasCanceled: true);
        }
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _isDisposed) != 0)
        {
            throw new ObjectDisposedException(nameof(CodingChatRoomRoleExecutor));
        }
    }
}

internal sealed class CodingChatRoomRoleExecutorFactory : IChatRoomRoleExecutorFactory
{
    private readonly string _languageServerCommand;

    internal CodingChatRoomRoleExecutorFactory(string languageServerCommand)
    {
        if (string.IsNullOrWhiteSpace(languageServerCommand))
        {
            throw new ArgumentException("Roslyn Language Server 命令不能为空。", nameof(languageServerCommand));
        }

        _languageServerCommand = languageServerCommand;
    }

    public ChatRoomRoleExecutionKind ExecutionKind => ChatRoomRoleExecutionKind.Coding;

    public IChatRoomRoleExecutor Create(ChatRoomRoleExecutorCreationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new CodingChatRoomRoleExecutor
        (
            new CodingAgent
            (
                new CodingAgentOptions
                {
                    LanguageServerCommand = _languageServerCommand,
                }
            )
        );
    }
}