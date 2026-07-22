using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

/// <summary>
/// 提供随代码工作区切换的编程工具集合。
/// </summary>
public sealed class CodingWorkspaceToolProvider : IAsyncDisposable
{
    private readonly ICodingWorkspaceToolSessionProvider _sessionProvider;
    private readonly object _sync = new();
    private readonly List<Task> _retiredSessionDisposals = [];
    private readonly HashSet<CodingWorkspaceTransaction> _transactions = [];
    private CodingWorkspaceToolSession? _session;
    private CodingWorkspaceTransaction? _appliedTransaction;
    private Task? _disposeTask;
    private bool _isDisposed;

    /// <summary>
    /// 创建代码工作区工具提供器。
    /// </summary>
    /// <param name="languageServerCommand">Roslyn Language Server 启动命令。</param>
    public CodingWorkspaceToolProvider(string languageServerCommand = "roslyn-language-server")
        : this(new CodingWorkspaceToolSessionProvider(languageServerCommand))
    {
    }

    internal CodingWorkspaceToolProvider(ICodingWorkspaceToolSessionProvider sessionProvider)
    {
        ArgumentNullException.ThrowIfNull(sessionProvider);
        _sessionProvider = sessionProvider;
    }

    /// <summary>
    /// 获取当前已发布的工作区路径。
    /// </summary>
    public string? WorkspacePath
    {
        get
        {
            lock (_sync)
            {
                return _session?.WorkspacePath;
            }
        }
    }

    /// <summary>
    /// 获取当前工作区的稳定工具租约。租约释放前，即使切换工作区，其底层资源也不会被释放。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>绑定当前工作区状态的工具租约；未设置工作区时返回空工具租约。</returns>
    public Task<CodingWorkspaceToolLease> AcquireLeaseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            ThrowIfDisposed();
            return Task.FromResult(_session?.AcquireLease() ?? new CodingWorkspaceToolLease(null));
        }
    }

    /// <summary>
    /// 准备一次工作区切换事务。准备完成不会改变当前已提交工作区。
    /// </summary>
    /// <param name="workspacePath">候选工作区路径；为空时准备清除当前工作区。</param>
    /// <param name="cancellationToken">取消令牌，仅影响候选资源准备。</param>
    /// <returns>独占候选资源的工作区切换事务。</returns>
    public async Task<IWorkspaceChangeTransaction> PrepareWorkspaceChangeAsync(
        string? workspacePath,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        CodingWorkspaceToolSession? candidateSession = string.IsNullOrWhiteSpace(workspacePath)
            ? null
            : await _sessionProvider.CreateAsync(workspacePath, cancellationToken).ConfigureAwait(false);
        lock (_sync)
        {
            if (!_isDisposed)
            {
                var transaction = new CodingWorkspaceTransaction(
                    this,
                    candidateSession,
                    candidateSession?.WorkspacePath);
                _transactions.Add(transaction);
                return transaction;
            }
        }

        if (candidateSession is not null)
        {
            await candidateSession.DisposeAsync().ConfigureAwait(false);
        }

        ThrowIfDisposed();
        throw new InvalidOperationException("工作区工具提供器释放状态异常。");
    }

    internal void ApplyTransaction(CodingWorkspaceTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        lock (_sync)
        {
            ThrowIfDisposed();
            if (transaction.State != CodingWorkspaceTransactionState.Prepared)
            {
                throw new InvalidOperationException($"工作区事务不能从 {transaction.State} 状态进入 Applied。 ");
            }
            if (_appliedTransaction is not null)
            {
                throw new InvalidOperationException("已有工作区事务处于 Applied 状态。");
            }

            transaction.State = CodingWorkspaceTransactionState.Applied;
            _appliedTransaction = transaction;
        }
    }

    internal Task RollbackTransactionAsync(
        CodingWorkspaceTransaction transaction,
        bool disposing)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        CodingWorkspaceToolSession? candidateSession;
        lock (_sync)
        {
            switch (transaction.State)
            {
                case CodingWorkspaceTransactionState.Prepared:
                    break;
                case CodingWorkspaceTransactionState.Applied:
                    if (!ReferenceEquals(_appliedTransaction, transaction))
                    {
                        throw new InvalidOperationException("Applied 工作区事务与 provider 屏障不一致。");
                    }

                    _appliedTransaction = null;
                    break;
                case CodingWorkspaceTransactionState.RolledBack:
                    return transaction.Completion.Task;
                case CodingWorkspaceTransactionState.Committed when disposing:
                    return Task.CompletedTask;
                case CodingWorkspaceTransactionState.Committed:
                    throw new InvalidOperationException("已发布的工作区事务不能回滚。");
                default:
                    throw new InvalidOperationException($"未知工作区事务状态：{transaction.State}。");
            }

            candidateSession = transaction.CandidateSession;
            transaction.CandidateSession = null;
            transaction.State = CodingWorkspaceTransactionState.RolledBack;
            _transactions.Remove(transaction);
        }

        return DisposeRolledBackCandidateAsync(transaction, candidateSession);
    }

    internal void CommitTransactionAfterPublish(CodingWorkspaceTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        lock (_sync)
        {
            if (transaction.State == CodingWorkspaceTransactionState.Committed)
            {
                return;
            }
            if (transaction.State != CodingWorkspaceTransactionState.Applied
                || !ReferenceEquals(_appliedTransaction, transaction))
            {
                throw new InvalidOperationException("只有当前 Applied 工作区事务可以在发布后提交。");
            }

            CodingWorkspaceToolSession? previousSession = _session;
            CodingWorkspaceToolSession? candidateSession = transaction.CandidateSession;
            transaction.CandidateSession = null;
            transaction.State = CodingWorkspaceTransactionState.Committed;
            _appliedTransaction = null;
            _transactions.Remove(transaction);

            if (_isDisposed)
            {
                _session = null;
                if (candidateSession is not null)
                {
                    TrackRetiredSession(candidateSession);
                }
            }
            else
            {
                _session = candidateSession;
            }

            if (previousSession is not null)
            {
                TrackRetiredSession(previousSession);
            }

            transaction.Completion.TrySetResult();
        }
    }

    /// <summary>
    /// 切换代码工作区，并重新创建绑定该工作区的工具。
    /// </summary>
    /// <param name="workspacePath">代码工作区路径；为空时清除当前工具。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SetWorkspacePathAsync(string? workspacePath, CancellationToken cancellationToken)
    {
        await using IWorkspaceChangeTransaction transaction = await PrepareWorkspaceChangeAsync(
            workspacePath,
            cancellationToken).ConfigureAwait(false);
        transaction.Apply();
        transaction.CommitAfterPublish();
    }

    /// <summary>
    /// 异步释放当前工作区工具占用的资源。
    /// </summary>
    public ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            if (_disposeTask is null)
            {
                _isDisposed = true;
                var preparedTransactions = new List<(
                    CodingWorkspaceTransaction Transaction,
                    CodingWorkspaceToolSession? CandidateSession)>();
                foreach (CodingWorkspaceTransaction transaction in _transactions
                             .Where(transaction => transaction.State == CodingWorkspaceTransactionState.Prepared)
                             .ToArray())
                {
                    preparedTransactions.Add((transaction, transaction.CandidateSession));
                    transaction.CandidateSession = null;
                    transaction.State = CodingWorkspaceTransactionState.RolledBack;
                    _transactions.Remove(transaction);
                }

                CodingWorkspaceToolSession? session = _session;
                _session = null;
                if (session is not null)
                {
                    TrackRetiredSession(session);
                }

                Task activeTransactionCompletion = _appliedTransaction?.Completion.Task
                    ?? Task.CompletedTask;
                _disposeTask = DisposeCoreAsync(activeTransactionCompletion, preparedTransactions);
            }

            return new ValueTask(_disposeTask);
        }
    }

    private void TrackRetiredSession(CodingWorkspaceToolSession session)
    {
        _retiredSessionDisposals.RemoveAll(task => task.IsCompletedSuccessfully);
        _retiredSessionDisposals.Add(session.Retire());
    }

    private static async Task DisposeRolledBackCandidateAsync(
        CodingWorkspaceTransaction transaction,
        CodingWorkspaceToolSession? candidateSession)
    {
        try
        {
            if (candidateSession is not null)
            {
                await candidateSession.DisposeAsync().ConfigureAwait(false);
            }

            transaction.Completion.TrySetResult();
        }
        catch (Exception ex)
        {
            transaction.Completion.TrySetException(ex);
            throw;
        }
    }

    private async Task DisposeCoreAsync(
        Task activeTransactionCompletion,
        IReadOnlyList<(
            CodingWorkspaceTransaction Transaction,
            CodingWorkspaceToolSession? CandidateSession)> preparedTransactions)
    {
        Task[] initialCleanupTasks =
        [
            activeTransactionCompletion,
            .. preparedTransactions.Select(item => DisposeRolledBackCandidateAsync(
                item.Transaction,
                item.CandidateSession)),
        ];
        Exception? initialCleanupException = null;
        try
        {
            await Task.WhenAll(initialCleanupTasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            initialCleanupException = ex;
        }

        Task[] retiredSessionDisposals;
        lock (_sync)
        {
            retiredSessionDisposals = _retiredSessionDisposals.ToArray();
        }

        Exception? retiredSessionException = null;
        try
        {
            await Task.WhenAll(retiredSessionDisposals).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            retiredSessionException = ex;
        }

        if (initialCleanupException is not null && retiredSessionException is not null)
        {
            throw new AggregateException(
                "释放工作区事务和已退休 Session 时均发生错误。",
                initialCleanupException,
                retiredSessionException);
        }
        if (initialCleanupException is not null)
        {
            throw initialCleanupException;
        }
        if (retiredSessionException is not null)
        {
            throw retiredSessionException;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(CodingWorkspaceToolProvider));
        }
    }
}

internal interface ICodingWorkspaceToolSessionProvider
{
    Task<CodingWorkspaceToolSession> CreateAsync(string workspacePath, CancellationToken cancellationToken);
}

internal sealed class CodingWorkspaceToolSessionProvider : ICodingWorkspaceToolSessionProvider
{
    private readonly string _languageServerCommand;

    internal CodingWorkspaceToolSessionProvider(string languageServerCommand)
    {
        if (string.IsNullOrWhiteSpace(languageServerCommand))
        {
            throw new ArgumentException("Roslyn Language Server 命令不能为空。", nameof(languageServerCommand));
        }

        _languageServerCommand = languageServerCommand;
    }

    public Task<CodingWorkspaceToolSession> CreateAsync(
        string workspacePath,
        CancellationToken cancellationToken) =>
        CodingWorkspaceToolSession.CreateAsync(workspacePath, _languageServerCommand, cancellationToken);
}
