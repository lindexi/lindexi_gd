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
    private long _workspaceChangeVersion;
    private int _isDisposed;

    internal CodingChatRoomRoleExecutor(CodingAgent codingAgent)
    {
        ArgumentNullException.ThrowIfNull(codingAgent);
        _codingAgent = codingAgent;
    }

    public ChatRoomRoleExecutionKind ExecutionKind => ChatRoomRoleExecutionKind.Coding;

    public async Task<ChatRoomRoleExecutionResult> RunAsync(
        ChatRoomRoleExecutionContext context,
        IReadOnlyList<AIContent> contents,
        CancellationToken cancellationToken)
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
            CodingAgentRunResult runResult = await _codingAgent.RunAsync(
                manualContext,
                contents,
                _workspacePath,
                cancellationToken).ConfigureAwait(false);
            return new ChatRoomRoleExecutionResult(
                runResult.AssistantChatMessage,
                CompleteAsync(runResult.CompletionTask, cancellationToken));
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task SetWorkspacePathAsync(
        CopilotChatManager chatManager,
        string? workspacePath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        ThrowIfDisposed();

        long workspaceChangeVersion = Interlocked.Increment(ref _workspaceChangeVersion);
        await using CodingWorkspaceToolCandidate candidate = await _codingAgent
            .CreateWorkspaceCandidateAsync(workspacePath, cancellationToken)
            .ConfigureAwait(false);

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            if (workspaceChangeVersion != Volatile.Read(ref _workspaceChangeVersion))
            {
                return;
            }

            string? oldChatManagerWorkspacePath = chatManager.WorkspacePath;
            string? oldWorkspacePath = _workspacePath;
            await _codingAgent.PublishWorkspaceCandidateAsync(
                candidate,
                () =>
                {
                    try
                    {
                        chatManager.WorkspacePath = candidate.WorkspacePath;
                        _workspacePath = candidate.WorkspacePath;
                    }
                    catch (Exception commitException)
                    {
                        _workspacePath = oldWorkspacePath;
                        try
                        {
                            chatManager.WorkspacePath = oldChatManagerWorkspacePath;
                        }
                        catch (Exception rollbackException)
                        {
                            throw new AggregateException(
                                "提交 Coding 工作区失败，且恢复 ChatManager 工作区路径失败。",
                                commitException,
                                rollbackException);
                        }

                        throw;
                    }
                },
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleLock.Release();
        }
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

    private async Task<ChatRoomRoleExecutionCompletion> CompleteAsync(
        Task<string?> completionTask,
        CancellationToken cancellationToken)
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
        return new CodingChatRoomRoleExecutor(new CodingAgent(_languageServerCommand));
    }
}
