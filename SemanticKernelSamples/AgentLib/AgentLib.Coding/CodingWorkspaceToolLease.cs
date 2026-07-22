using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

/// <summary>
/// 固定一次编程运行所使用的工作区路径、工具集合和底层资源生命周期。
/// 工作区切换不能立即释放旧 Language Server，否则仍在运行的模型工具调用会访问已释放资源；
/// 此类型通过显式租约让旧 Session 延迟到最后一次运行结束后再释放。
/// </summary>
public sealed class CodingWorkspaceToolLease : IDisposable, IAsyncDisposable
{
    private CodingWorkspaceToolSession? _session;

    internal CodingWorkspaceToolLease(CodingWorkspaceToolSession? session)
    {
        _session = session;
        WorkspacePath = session?.WorkspacePath;
        Tools = session?.Tools ?? [];
    }

    /// <summary>
    /// 本租约绑定的工作区路径。未设置工作区时为 <see langword="null"/>。
    /// </summary>
    public string? WorkspacePath { get; }

    /// <summary>
    /// 本租约绑定的不可变编程工具集合。
    /// </summary>
    public IReadOnlyList<AITool> Tools { get; }

    /// <summary>
    /// 释放本次运行对工作区资源的引用。
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 异步释放本次运行对工作区资源的引用。
    /// </summary>
    public ValueTask DisposeAsync()
    {
        CodingWorkspaceToolSession? session = Interlocked.Exchange(ref _session, null);
        return session is null ? default : session.ReleaseLeaseAsync();
    }
}
