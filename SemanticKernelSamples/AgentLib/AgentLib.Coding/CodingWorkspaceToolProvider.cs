using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

/// <summary>
/// 提供随代码工作区切换的编程工具集合。
/// </summary>
public sealed class CodingWorkspaceToolProvider : IAsyncDisposable
{
    private readonly string _languageServerCommand;
    private readonly Func<string, string, CancellationToken, Task<CodingWorkspaceToolSession>> _sessionFactory;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly object _disposeSync = new();
    private readonly List<Task> _retiredSessionDisposals = [];
    private CodingWorkspaceToolSession? _session;
    private Task? _disposeTask;
    private int _isDisposed;
    private long _workspaceChangeVersion;

    /// <summary>
    /// 创建代码工作区工具提供器。
    /// </summary>
    /// <param name="languageServerCommand">Roslyn Language Server 启动命令。</param>
    public CodingWorkspaceToolProvider(string languageServerCommand = "roslyn-language-server")
        : this(languageServerCommand, CodingWorkspaceToolSession.CreateAsync)
    {
    }

    internal CodingWorkspaceToolProvider(
        string languageServerCommand,
        Func<string, string, CancellationToken, Task<CodingWorkspaceToolSession>> sessionFactory)
    {
        if (string.IsNullOrWhiteSpace(languageServerCommand))
        {
            throw new ArgumentException("Roslyn Language Server 命令不能为空。", nameof(languageServerCommand));
        }
        ArgumentNullException.ThrowIfNull(sessionFactory);

        _languageServerCommand = languageServerCommand;
        _sessionFactory = sessionFactory;
    }

    /// <summary>
    /// 获取当前工作区已发布的 AI 工具。
    /// </summary>
    public IReadOnlyList<AITool> AITools => Volatile.Read(ref _session)?.Tools ?? [];

    /// <summary>
    /// 获取当前已发布的工作区路径。
    /// </summary>
    public string? WorkspacePath => Volatile.Read(ref _session)?.WorkspacePath;

    /// <summary>
    /// 获取当前工作区的稳定工具租约。租约释放前，即使切换工作区，其底层资源也不会被释放。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>绑定当前工作区状态的工具租约；未设置工作区时返回空工具租约。</returns>
    public async Task<CodingWorkspaceToolLease> AcquireLeaseAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            return _session?.AcquireLease() ?? new CodingWorkspaceToolLease(null);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    internal async Task<CodingWorkspaceToolCandidate> CreateCandidateAsync(
        string? workspacePath,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        CodingWorkspaceToolSession? candidateSession = string.IsNullOrWhiteSpace(workspacePath)
            ? null
            : await _sessionFactory(
                workspacePath,
                _languageServerCommand,
                cancellationToken).ConfigureAwait(false);
        return new CodingWorkspaceToolCandidate(candidateSession);
    }

    internal async Task PublishCandidateAsync(
        CodingWorkspaceToolCandidate candidate,
        Action? commit = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            CodingWorkspaceToolSession? candidateSession = candidate.TakeSession();
            CodingWorkspaceToolSession? sessionToRetire = _session;
            try
            {
                commit?.Invoke();
                _session = candidateSession;
                candidateSession = null;
            }
            finally
            {
                if (candidateSession is not null)
                {
                    await candidateSession.DisposeAsync().ConfigureAwait(false);
                }
            }

            if (sessionToRetire is not null)
            {
                TrackRetiredSession(sessionToRetire);
            }
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <summary>
    /// 切换代码工作区，并重新创建绑定该工作区的工具。
    /// </summary>
    /// <param name="workspacePath">代码工作区路径；为空时清除当前工具。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SetWorkspacePathAsync(string? workspacePath, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        long workspaceChangeVersion = Interlocked.Increment(ref _workspaceChangeVersion);
        await using CodingWorkspaceToolCandidate candidate = await CreateCandidateAsync(
            workspacePath,
            cancellationToken).ConfigureAwait(false);

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            if (workspaceChangeVersion != Volatile.Read(ref _workspaceChangeVersion))
            {
                return;
            }

            CodingWorkspaceToolSession? sessionToRetire = _session;
            _session = candidate.TakeSession();
            if (sessionToRetire is not null)
            {
                TrackRetiredSession(sessionToRetire);
            }
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <summary>
    /// 异步释放当前工作区工具占用的资源。
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
        Task[] disposalTasks;
        await _lifecycleLock.WaitAsync().ConfigureAwait(false);
        try
        {

            CodingWorkspaceToolSession? session = _session;
            _session = null;
            if (session is not null)
            {
                TrackRetiredSession(session);
            }

            disposalTasks = [.. _retiredSessionDisposals];
        }
        finally
        {
            _lifecycleLock.Release();
        }

        await Task.WhenAll(disposalTasks).ConfigureAwait(false);
    }

    private void TrackRetiredSession(CodingWorkspaceToolSession session)
    {
        _retiredSessionDisposals.RemoveAll(task => task.IsCompletedSuccessfully);
        _retiredSessionDisposals.Add(session.Retire());
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _isDisposed) != 0)
        {
            throw new ObjectDisposedException(nameof(CodingWorkspaceToolProvider));
        }
    }
}
