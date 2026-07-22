using AgentLib.ChatRoom.Domain;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using System.Text.Json;

using LegacyRoleDefinition = AgentLib.ChatRoom.Model.ChatRoomRoleDefinition;
using LegacyExecutionKind = AgentLib.ChatRoom.Model.ChatRoomRoleExecutionKind;
using LegacyParticipationMode = AgentLib.ChatRoom.Model.ChatRoomParticipationMode;

namespace AgentLib.ChatRoom.Runtime;

internal sealed class IsolatedChatRoomRoleRuntime : IChatRoomRoleRuntime
{
    private readonly ChatRoomRoleFactory _roleFactory;
    private readonly IReadOnlyDictionary<string, ILanguageModelProvider> _languageModelProviders;
    private readonly string? _defaultPrimaryModelId;
    private readonly object _disposeSync = new();
    private readonly object _workspaceSync = new();
    private Task? _disposeTask;
    private string? _workspacePath;
    private int _activeExecutionCount;
    private int _isDisposed;

    internal IsolatedChatRoomRoleRuntime(
        ChatRoomRoleDefinition definition,
        ChatRoomRoleFactory roleFactory,
        IReadOnlyDictionary<string, ILanguageModelProvider> languageModelProviders,
        string? defaultPrimaryModelId)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(roleFactory);
        ArgumentNullException.ThrowIfNull(languageModelProviders);

        Identity = definition.Identity;
        ExecutionKind = definition.ExecutionKind;
        RuntimeVersion = definition.RuntimeVersion;
        _roleFactory = roleFactory;
        _languageModelProviders = languageModelProviders;
        _defaultPrimaryModelId = string.IsNullOrWhiteSpace(defaultPrimaryModelId)
            ? null
            : defaultPrimaryModelId.Trim();
    }

    public ChatRoomRoleIdentity Identity { get; }

    public ChatRoomRoleExecutionKind ExecutionKind { get; }

    public long RuntimeVersion { get; }

    public async Task<ChatRoomRoleExecutionCandidate> ExecuteAsync(
        ChatRoomRoleExecutionRequest request,
        IChatRoomRoleExecutionEventSink? eventSink,
        CancellationToken cancellationToken)
    {
        _ = eventSink;
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);
        EnterExecution();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            LegacyRoleDefinition definition = MapDefinition(request.Definition);
            await using ChatRoomRole role = _roleFactory.CreateRole(definition);
            ChatRoomRoleModelBinder.RegisterProvidersAndSelectPrimary(
                role,
                _languageModelProviders,
                _defaultPrimaryModelId);
            await role.InitializeAsync(cancellationToken).ConfigureAwait(false);
            string? workspacePath;
            lock (_workspaceSync)
            {
                workspacePath = _workspacePath;
            }
            if (!AreSameWorkspace(workspacePath, request.WorkspacePath))
            {
                throw new InvalidOperationException("执行请求与运行时已发布工作区不一致。");
            }
            await role.SetWorkspacePathAsync(workspacePath, cancellationToken).ConfigureAwait(false);
            await RestoreCheckpointAsync(role, request.CommittedCheckpoint, cancellationToken).ConfigureAwait(false);

            IReadOnlyList<string> incrementalMessages = BuildIncrementalMessages(
                request.InputMessages,
                request.Definition.Identity.RoleId);
            if (incrementalMessages.Count == 0)
            {
                throw new ArgumentException("角色执行至少需要一条其他参与者的公开消息。", nameof(request));
            }

            ChatRoomSpeakResult? speakResult = await role
                .SpeakAsync(incrementalMessages, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (speakResult is null)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            string? publicContent = await speakResult.FinalContentTask.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            JsonElement? serializedState = await role
                .SerializeAgentSessionStateAsync(cancellationToken)
                .ConfigureAwait(false);
            if (serializedState is null)
            {
                throw new InvalidOperationException("角色执行完成后未生成可提交的 AgentSession checkpoint。");
            }

            byte[] checkpointPayload = JsonSerializer.SerializeToUtf8Bytes(serializedState.Value);
            var candidateCheckpoint = new ChatRoomRoleCheckpointCandidate(
                Identity,
                request.Definition.RuntimeVersion,
                ExecutionKind,
                sessionRevision: (request.CommittedCheckpoint?.SessionRevision ?? 0) + 1,
                consumedThroughSequence: request.InputThroughSequence,
                serializerVersion: 1,
                ChatRoomRoleCheckpointFormats.AgentSessionJsonV1,
                checkpointPayload);
            return new ChatRoomRoleExecutionCandidate(
                request.ExecutionId,
                Identity,
                publicContent,
                speakResult.ModelDisplayName,
                candidateCheckpoint);
        }
        finally
        {
            ExitExecution();
        }
    }

    public Task<IChatRoomRoleWorkspaceTransaction> PrepareWorkspaceChangeAsync(
        string? workspacePath,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        IChatRoomRoleWorkspaceTransaction transaction = new IsolatedWorkspaceTransaction(
            this,
            NormalizeWorkspacePath(workspacePath));
        return Task.FromResult(transaction);
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _isDisposed) != 0)
        {
            throw new ObjectDisposedException(nameof(IsolatedChatRoomRoleRuntime));
        }
    }

    private string? ReadWorkspacePath()
    {
        lock (_workspaceSync)
        {
            return _workspacePath;
        }
    }

    private void PublishWorkspacePath(string? workspacePath)
    {
        lock (_workspaceSync)
        {
            ThrowIfDisposed();
            _workspacePath = workspacePath;
        }
    }

    private static string? NormalizeWorkspacePath(string? workspacePath) =>
        string.IsNullOrWhiteSpace(workspacePath) ? null : Path.GetFullPath(workspacePath);

    private static bool AreSameWorkspace(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private sealed class IsolatedWorkspaceTransaction(
        IsolatedChatRoomRoleRuntime owner,
        string? workspacePath) : IChatRoomRoleWorkspaceTransaction
    {
        private readonly string? _previousWorkspacePath = owner.ReadWorkspacePath();
        private WorkspaceTransactionState _state = WorkspaceTransactionState.Prepared;

        public string? WorkspacePath { get; } = workspacePath;

        public void Apply()
        {
            if (_state != WorkspaceTransactionState.Prepared)
            {
                throw new InvalidOperationException($"工作区事务不能从 {_state} 进入 Applied。");
            }

            owner.PublishWorkspacePath(WorkspacePath);
            _state = WorkspaceTransactionState.Applied;
        }

        public ValueTask RollbackAsync()
        {
            switch (_state)
            {
                case WorkspaceTransactionState.Prepared:
                    _state = WorkspaceTransactionState.RolledBack;
                    return default;
                case WorkspaceTransactionState.Applied:
                    owner.PublishWorkspacePath(_previousWorkspacePath);
                    _state = WorkspaceTransactionState.RolledBack;
                    return default;
                case WorkspaceTransactionState.RolledBack:
                    return default;
                case WorkspaceTransactionState.Committed:
                    throw new InvalidOperationException("已发布的工作区事务不能回滚。");
                default:
                    throw new InvalidOperationException($"未知工作区事务状态：{_state}。");
            }
        }

        public void CommitAfterPublish()
        {
            if (_state == WorkspaceTransactionState.Committed)
            {
                return;
            }
            if (_state != WorkspaceTransactionState.Applied)
            {
                throw new InvalidOperationException("只有 Applied 工作区事务可以提交。");
            }

            _state = WorkspaceTransactionState.Committed;
        }

        public ValueTask DisposeAsync()
        {
            return _state is WorkspaceTransactionState.Prepared or WorkspaceTransactionState.Applied
                ? RollbackAsync()
                : default;
        }
    }

    private enum WorkspaceTransactionState
    {
        Prepared,
        Applied,
        RolledBack,
        Committed,
    }

    public ValueTask DisposeAsync()
    {
        lock (_disposeSync)
        {
            if (_disposeTask is null)
            {
                Volatile.Write(ref _isDisposed, 1);
                _disposeTask = WaitForExecutionsAsync();
            }

            return new ValueTask(_disposeTask);
        }
    }

    private void ValidateRequest(ChatRoomRoleExecutionRequest request)
    {
        if (request.Definition.Identity != Identity)
        {
            throw new ArgumentException("执行请求与运行时角色身份不一致。", nameof(request));
        }
        if (request.Definition.ExecutionKind != ExecutionKind)
        {
            throw new ArgumentException("执行请求与运行时执行种类不一致。", nameof(request));
        }
        if (request.Definition.RuntimeVersion != RuntimeVersion)
        {
            throw new ArgumentException("执行请求与运行时版本不一致。", nameof(request));
        }
        if (request.Definition.IsHuman)
        {
            throw new ArgumentException("人类角色不能创建模型执行候选。", nameof(request));
        }
    }

    private static async Task RestoreCheckpointAsync(
        ChatRoomRole role,
        ChatRoomRoleCheckpoint? checkpoint,
        CancellationToken cancellationToken)
    {
        if (checkpoint is null)
        {
            return;
        }
        if (!string.Equals(
                checkpoint.Format,
                ChatRoomRoleCheckpointFormats.AgentSessionJsonV1,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException($"不支持的角色 checkpoint 格式：{checkpoint.Format}。");
        }
        if (checkpoint.SerializerVersion != 1)
        {
            throw new InvalidDataException($"不支持的角色 checkpoint 序列化器版本：{checkpoint.SerializerVersion}。");
        }

        using JsonDocument document = JsonDocument.Parse(checkpoint.Payload);
        await role.RestoreAgentSessionStateAsync(
            document.RootElement.Clone(),
            cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<string> BuildIncrementalMessages(
        IReadOnlyList<ChatRoomMessage> messages,
        string roleId)
    {
        var values = new List<string>(messages.Count);
        foreach (ChatRoomMessage message in messages)
        {
            if (message.Kind == ChatRoomMessageKind.System
                || string.Equals(message.SenderRoleId, roleId, StringComparison.Ordinal))
            {
                continue;
            }

            string prefix = message.Kind == ChatRoomMessageKind.Human
                ? "用户"
                : string.IsNullOrWhiteSpace(message.SenderRoleName)
                    ? "另一位参与者"
                    : message.SenderRoleName;
            values.Add($"{prefix}说：{message.Content}");
        }

        return values;
    }

    private static LegacyRoleDefinition MapDefinition(ChatRoomRoleDefinition definition) => new()
    {
        RoleId = definition.Identity.RoleId,
        ExecutionKind = definition.ExecutionKind == ChatRoomRoleExecutionKind.Standard
            ? LegacyExecutionKind.Standard
            : LegacyExecutionKind.Coding,
        RoleName = definition.RoleName,
        SystemPrompt = definition.SystemPrompt,
        IsHuman = definition.IsHuman,
        ModelProviderId = definition.ModelProviderId,
        ModelId = definition.ModelId,
        SkillFolders = definition.SkillFolders.ToList(),
        MemoryContent = definition.MemoryContent,
        ParticipationMode = definition.ParticipationMode == ChatRoomParticipationMode.AlwaysParticipate
            ? LegacyParticipationMode.AlwaysParticipate
            : LegacyParticipationMode.MentionOnly,
        IsManagerRole = definition.IsManagerRole,
    };

    private void EnterExecution()
    {
        if (Volatile.Read(ref _isDisposed) != 0)
        {
            throw new ObjectDisposedException(nameof(IsolatedChatRoomRoleRuntime));
        }

        Interlocked.Increment(ref _activeExecutionCount);
        if (Volatile.Read(ref _isDisposed) != 0)
        {
            ExitExecution();
            throw new ObjectDisposedException(nameof(IsolatedChatRoomRoleRuntime));
        }
    }

    private void ExitExecution()
    {
        Interlocked.Decrement(ref _activeExecutionCount);
    }

    private async Task WaitForExecutionsAsync()
    {
        while (Volatile.Read(ref _activeExecutionCount) != 0)
        {
            await Task.Delay(10).ConfigureAwait(false);
        }
    }
}
