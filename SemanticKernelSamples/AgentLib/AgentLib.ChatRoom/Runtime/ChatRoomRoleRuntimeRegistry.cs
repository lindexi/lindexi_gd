using AgentLib.ChatRoom.Domain;

namespace AgentLib.ChatRoom.Runtime;

/// <summary>
/// 固定一次角色执行所使用的运行时实例，并在释放前阻止其被销毁。
/// </summary>
public sealed class ChatRoomRoleRuntimeLease : IAsyncDisposable
{
    private ChatRoomRoleRuntimeEntry? _entry;

    internal ChatRoomRoleRuntimeLease(ChatRoomRoleRuntimeEntry entry)
    {
        _entry = entry;
        Identity = entry.Runtime.Identity;
        ExecutionKind = entry.Runtime.ExecutionKind;
        RuntimeVersion = entry.Runtime.RuntimeVersion;
    }

    /// <summary>
    /// 租约绑定的角色身份。
    /// </summary>
    public ChatRoomRoleIdentity Identity { get; }

    /// <summary>
    /// 租约绑定的执行种类。
    /// </summary>
    public ChatRoomRoleExecutionKind ExecutionKind { get; }

    /// <summary>
    /// 租约绑定的运行时版本。
    /// </summary>
    public long RuntimeVersion { get; }

    /// <summary>
    /// 使用租约绑定的运行时执行一次候选计算。
    /// </summary>
    public Task<ChatRoomRoleExecutionCandidate> ExecuteAsync(
        ChatRoomRoleExecutionRequest request,
        IChatRoomRoleExecutionEventSink? eventSink,
        CancellationToken cancellationToken)
    {
        ChatRoomRoleRuntimeEntry entry = Volatile.Read(ref _entry)
            ?? throw new ObjectDisposedException(nameof(ChatRoomRoleRuntimeLease));
        return entry.Runtime.ExecuteAsync(request, eventSink, cancellationToken);
    }

    /// <summary>
    /// 使用租约绑定的运行时准备工作区切换。
    /// </summary>
    public Task<IChatRoomRoleWorkspaceTransaction> PrepareWorkspaceChangeAsync(
        string? workspacePath,
        CancellationToken cancellationToken = default)
    {
        ChatRoomRoleRuntimeEntry entry = Volatile.Read(ref _entry)
            ?? throw new ObjectDisposedException(nameof(ChatRoomRoleRuntimeLease));
        return entry.Runtime.PrepareWorkspaceChangeAsync(workspacePath, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        ChatRoomRoleRuntimeEntry? entry = Interlocked.Exchange(ref _entry, null);
        return entry is null ? default : entry.ReleaseLeaseAsync();
    }
}

/// <summary>
/// 唯一持有房间角色运行时，并通过租约协调替换、删除和关闭。
/// </summary>
public sealed class ChatRoomRoleRuntimeRegistry : IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly IReadOnlyDictionary<ChatRoomRoleExecutionKind, IChatRoomRoleRuntimeFactory> _factories;
    private readonly Dictionary<string, ChatRoomRoleRuntimeEntry> _entries = new(StringComparer.Ordinal);
    private readonly List<Task> _retiredDisposals = [];
    private Task? _disposeTask;
    private bool _isDisposed;

    /// <summary>
    /// 创建运行时注册表。
    /// </summary>
    public ChatRoomRoleRuntimeRegistry(IEnumerable<IChatRoomRoleRuntimeFactory> factories)
    {
        ArgumentNullException.ThrowIfNull(factories);
        var values = new Dictionary<ChatRoomRoleExecutionKind, IChatRoomRoleRuntimeFactory>();
        foreach (IChatRoomRoleRuntimeFactory factory in factories)
        {
            ArgumentNullException.ThrowIfNull(factory);
            if (!Enum.IsDefined(typeof(ChatRoomRoleExecutionKind), factory.ExecutionKind))
            {
                throw new ArgumentOutOfRangeException(nameof(factories));
            }
            if (!values.TryAdd(factory.ExecutionKind, factory))
            {
                throw new ArgumentException($"执行种类 {factory.ExecutionKind} 注册了多个运行时工厂。", nameof(factories));
            }
        }

        _factories = values;
    }

    /// <summary>
    /// 创建并注册角色运行时。
    /// </summary>
    public async Task AddAsync(ChatRoomRoleDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.IsHuman)
        {
            throw new ArgumentException("人类角色不注册模型运行时。", nameof(definition));
        }
        ThrowIfDisposed();
        IChatRoomRoleRuntime runtime = await CreateRuntimeAsync(definition, cancellationToken).ConfigureAwait(false);
        Exception? rejection = null;
        Task? rejectedRuntimeDisposal = null;
        lock (_sync)
        {
            if (_isDisposed)
            {
                rejectedRuntimeDisposal = new ChatRoomRoleRuntimeEntry(runtime).Retire();
                rejection = new ObjectDisposedException(nameof(ChatRoomRoleRuntimeRegistry));
            }
            else if (_entries.ContainsKey(definition.Identity.RoleId))
            {
                rejectedRuntimeDisposal = new ChatRoomRoleRuntimeEntry(runtime).Retire();
                rejection = new InvalidOperationException($"角色 {definition.Identity.RoleId} 已存在运行时。");
            }
            else
            {
                _entries.Add(definition.Identity.RoleId, new ChatRoomRoleRuntimeEntry(runtime));
            }
        }

        if (rejection is not null)
        {
            await rejectedRuntimeDisposal!.ConfigureAwait(false);
            throw rejection;
        }
    }

    /// <summary>
    /// 以新 identity 或 runtime version 原子替换角色运行时。
    /// </summary>
    public async Task ReplaceAsync(ChatRoomRoleDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.IsHuman)
        {
            throw new ArgumentException("人类角色不注册模型运行时。", nameof(definition));
        }
        ThrowIfDisposed();
        IChatRoomRoleRuntime runtime = await CreateRuntimeAsync(definition, cancellationToken).ConfigureAwait(false);
        Exception? rejection = null;
        Task? rejectedRuntimeDisposal = null;
        lock (_sync)
        {
            if (_isDisposed)
            {
                rejectedRuntimeDisposal = new ChatRoomRoleRuntimeEntry(runtime).Retire();
                rejection = new ObjectDisposedException(nameof(ChatRoomRoleRuntimeRegistry));
            }
            else if (!_entries.TryGetValue(definition.Identity.RoleId, out ChatRoomRoleRuntimeEntry? previous))
            {
                rejectedRuntimeDisposal = new ChatRoomRoleRuntimeEntry(runtime).Retire();
                rejection = new KeyNotFoundException($"角色 {definition.Identity.RoleId} 不存在运行时。");
            }
            else
            {
                _entries[definition.Identity.RoleId] = new ChatRoomRoleRuntimeEntry(runtime);
                TrackRetired(previous);
            }
        }

        if (rejection is not null)
        {
            await rejectedRuntimeDisposal!.ConfigureAwait(false);
            throw rejection;
        }
    }

    /// <summary>
    /// 获取与指定角色事实完全匹配的执行租约。
    /// </summary>
    public ChatRoomRoleRuntimeLease Acquire(ChatRoomRoleDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        lock (_sync)
        {
            ThrowIfDisposed();
            if (!_entries.TryGetValue(definition.Identity.RoleId, out ChatRoomRoleRuntimeEntry? entry))
            {
                throw new KeyNotFoundException($"角色 {definition.Identity.RoleId} 不存在运行时。");
            }
            if (entry.Runtime.Identity != definition.Identity
                || entry.Runtime.ExecutionKind != definition.ExecutionKind
                || entry.Runtime.RuntimeVersion != definition.RuntimeVersion)
            {
                throw new InvalidOperationException("角色定义与当前运行时事实不一致。");
            }

            return entry.AcquireLease();
        }
    }

    /// <summary>
    /// 删除角色运行时；已有执行租约释放后才真正销毁。
    /// </summary>
    public Task RemoveAsync(string roleId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("角色标识不能为空或空白。", nameof(roleId));
        }

        lock (_sync)
        {
            ThrowIfDisposed();
            if (!_entries.Remove(roleId.Trim(), out ChatRoomRoleRuntimeEntry? entry))
            {
                return Task.CompletedTask;
            }

            return TrackRetired(entry);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            if (_disposeTask is null)
            {
                _isDisposed = true;
                ChatRoomRoleRuntimeEntry[] entries = _entries.Values.ToArray();
                _entries.Clear();
                Task[] existingRetired = _retiredDisposals.ToArray();
                _disposeTask = DisposeCoreAsync(entries, existingRetired);
            }

            return new ValueTask(_disposeTask);
        }
    }

    private async Task<IChatRoomRoleRuntime> CreateRuntimeAsync(
        ChatRoomRoleDefinition definition,
        CancellationToken cancellationToken)
    {
        if (!_factories.TryGetValue(definition.ExecutionKind, out IChatRoomRoleRuntimeFactory? factory))
        {
            throw new InvalidOperationException($"没有为执行种类 {definition.ExecutionKind} 注册运行时工厂。");
        }

        IChatRoomRoleRuntime runtime = await factory.CreateAsync(definition, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("角色运行时工厂返回了空运行时。");
        if (runtime.Identity != definition.Identity
            || runtime.ExecutionKind != definition.ExecutionKind
            || runtime.RuntimeVersion != definition.RuntimeVersion)
        {
            await runtime.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException("角色运行时工厂返回了与定义事实不一致的运行时。");
        }

        return runtime;
    }

    private Task TrackRetired(ChatRoomRoleRuntimeEntry entry)
    {
        _retiredDisposals.RemoveAll(task => task.IsCompletedSuccessfully);
        Task disposal = entry.Retire();
        _retiredDisposals.Add(disposal);
        return disposal;
    }

    private static async Task DisposeCoreAsync(
        IReadOnlyList<ChatRoomRoleRuntimeEntry> entries,
        IReadOnlyList<Task> existingRetired)
    {
        Task[] retirements = entries.Select(entry => entry.Retire()).ToArray();
        await Task.WhenAll(existingRetired.Concat(retirements)).ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ChatRoomRoleRuntimeRegistry));
        }
    }
}

internal sealed class ChatRoomRoleRuntimeEntry
{
    private readonly object _sync = new();
    private readonly TaskCompletionSource _disposalCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _leaseCount;
    private bool _isRetired;
    private bool _isDisposalStarted;

    internal ChatRoomRoleRuntimeEntry(IChatRoomRoleRuntime runtime)
    {
        Runtime = runtime;
    }

    internal IChatRoomRoleRuntime Runtime { get; }

    internal ChatRoomRoleRuntimeLease AcquireLease()
    {
        lock (_sync)
        {
            if (_isRetired)
            {
                throw new ObjectDisposedException(nameof(ChatRoomRoleRuntimeEntry));
            }

            _leaseCount++;
            return new ChatRoomRoleRuntimeLease(this);
        }
    }

    internal ValueTask ReleaseLeaseAsync()
    {
        Task? disposalTask = null;
        lock (_sync)
        {
            if (_leaseCount <= 0)
            {
                throw new InvalidOperationException("角色运行时租约已全部释放。");
            }

            _leaseCount--;
            if (_isRetired && _leaseCount == 0)
            {
                disposalTask = StartDisposalLocked();
            }
        }

        return disposalTask is null ? default : new ValueTask(disposalTask);
    }

    internal Task Retire()
    {
        lock (_sync)
        {
            _isRetired = true;
            if (_leaseCount == 0)
            {
                _ = StartDisposalLocked();
            }

            return _disposalCompletion.Task;
        }
    }

    private Task StartDisposalLocked()
    {
        if (!_isDisposalStarted)
        {
            _isDisposalStarted = true;
            _ = DisposeRuntimeAsync();
        }

        return _disposalCompletion.Task;
    }

    private async Task DisposeRuntimeAsync()
    {
        try
        {
            await Runtime.DisposeAsync().ConfigureAwait(false);
            _disposalCompletion.TrySetResult();
        }
        catch (Exception ex)
        {
            _disposalCompletion.TrySetException(ex);
        }
    }
}
