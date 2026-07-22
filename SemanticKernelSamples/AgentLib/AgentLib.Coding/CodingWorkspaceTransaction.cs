namespace AgentLib.Coding;

/// <summary>
/// 一次代码工作区切换事务。
/// </summary>
public interface IWorkspaceChangeTransaction : IAsyncDisposable
{
    /// <summary>
    /// 候选工作区路径；为空表示清除当前工作区。
    /// </summary>
    string? WorkspacePath { get; }

    /// <summary>
    /// 应用已准备的候选并建立发布前屏障。
    /// </summary>
    void Apply();

    /// <summary>
    /// 回滚尚未发布的工作区切换并释放候选资源。
    /// </summary>
    ValueTask RollbackAsync();

    /// <summary>
    /// 在调用方已经发布新状态后完成提交。
    /// 重复调用保持幂等。
    /// </summary>
    void CommitAfterPublish();
}

internal sealed class CodingWorkspaceTransaction : IWorkspaceChangeTransaction
{
    private readonly CodingWorkspaceToolProvider _provider;

    internal CodingWorkspaceTransaction(
        CodingWorkspaceToolProvider provider,
        CodingWorkspaceToolSession? candidateSession,
        string? workspacePath)
    {
        _provider = provider;
        CandidateSession = candidateSession;
        WorkspacePath = workspacePath;
    }

    /// <summary>
    /// 候选工作区路径；为空表示清除当前工作区。
    /// </summary>
    public string? WorkspacePath { get; }

    internal CodingWorkspaceToolSession? CandidateSession { get; set; }

    internal CodingWorkspaceTransactionState State { get; set; } = CodingWorkspaceTransactionState.Prepared;

    internal TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// 应用已准备的工作区候选并建立发布前屏障。
    /// 在调用 <see cref="CommitAfterPublish"/> 前，新的工作区不会成为 provider 的公开已提交状态。
    /// </summary>
    public void Apply()
    {
        _provider.ApplyTransaction(this);
    }

    /// <summary>
    /// 回滚尚未发布的工作区切换并释放候选资源。
    /// </summary>
    public ValueTask RollbackAsync()
    {
        return new ValueTask(_provider.RollbackTransactionAsync(this, disposing: false));
    }

    /// <summary>
    /// 在调用方已经发布新状态后提交工作区切换。
    /// 此方法不可取消且幂等；成功后旧资源进入退休流程，事务不再允许回滚。
    /// </summary>
    public void CommitAfterPublish()
    {
        _provider.CommitTransactionAfterPublish(this);
    }

    /// <summary>
    /// 释放未提交事务。Prepared 事务会丢弃候选，Applied 事务会自动回滚；
    /// 已提交事务的释放为空操作。
    /// </summary>
    public ValueTask DisposeAsync()
    {
        return new ValueTask(_provider.RollbackTransactionAsync(this, disposing: true));
    }
}

internal enum CodingWorkspaceTransactionState
{
    Prepared,
    Applied,
    RolledBack,
    Committed,
}
