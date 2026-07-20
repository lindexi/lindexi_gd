using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

/// <summary>
/// 提供随代码工作区切换的编程工具集合。
/// </summary>
public sealed class CodingWorkspaceToolProvider : IAsyncDisposable
{
    private readonly string _languageServerCommand;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private CodingWorkspaceToolSession? _session;
    private int _isDisposed;

    /// <summary>
    /// 创建代码工作区工具提供器。
    /// </summary>
    /// <param name="languageServerCommand">Roslyn Language Server 启动命令。</param>
    public CodingWorkspaceToolProvider(string languageServerCommand = "roslyn-language-server")
    {
        if (string.IsNullOrWhiteSpace(languageServerCommand))
        {
            throw new ArgumentException("Roslyn Language Server 命令不能为空。", nameof(languageServerCommand));
        }

        _languageServerCommand = languageServerCommand;
    }

    /// <summary>
    /// 获取当前工作区已发布的 AI 工具。
    /// </summary>
    public IReadOnlyList<AITool> AITools => _session?.Tools ?? [];

    /// <summary>
    /// 切换代码工作区，并重新创建绑定该工作区的工具。
    /// </summary>
    /// <param name="workspacePath">代码工作区路径；为空时清除当前工具。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SetWorkspacePathAsync(string? workspacePath, CancellationToken cancellationToken)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                throw new ObjectDisposedException(nameof(CodingWorkspaceToolProvider));
            }

            CodingWorkspaceToolSession? candidateSession = string.IsNullOrWhiteSpace(workspacePath)
                ? null
                : await CodingWorkspaceToolSession.CreateAsync(
                    workspacePath,
                    _languageServerCommand,
                    cancellationToken).ConfigureAwait(false);
            CodingWorkspaceToolSession? oldSession = _session;
            _session = candidateSession;
            if (oldSession is not null)
            {
                await oldSession.DisposeAsync().ConfigureAwait(false);
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
    public async ValueTask DisposeAsync()
    {
        await _lifecycleLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            {
                return;
            }

            CodingWorkspaceToolSession? session = _session;
            _session = null;
            if (session is not null)
            {
                await session.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }
}
