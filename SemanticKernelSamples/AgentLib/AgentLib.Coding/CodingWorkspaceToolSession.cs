using AgentLib.Tools;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal sealed class CodingWorkspaceToolSession : IAsyncDisposable
{
    private readonly IAsyncDisposable? _asyncDisposable;
    private readonly object _lifecycleLock = new();
    private readonly TaskCompletionSource _disposalCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _leaseCount;
    private bool _isRetired;
    private bool _isDisposalStarted;

    internal CodingWorkspaceToolSession(
        string workspacePath,
        IReadOnlyList<AITool> tools,
        IAsyncDisposable? asyncDisposable = null)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            throw new ArgumentException("工作区路径不能为空。", nameof(workspacePath));
        }
        ArgumentNullException.ThrowIfNull(tools);

        WorkspacePath = workspacePath;
        _asyncDisposable = asyncDisposable;
        Tools = Array.AsReadOnly(tools.ToArray());
    }

    public string WorkspacePath { get; }

    public IReadOnlyList<AITool> Tools { get; }

    internal int LeaseCount
    {
        get
        {
            lock (_lifecycleLock)
            {
                return _leaseCount;
            }
        }
    }

    internal bool IsRetired
    {
        get
        {
            lock (_lifecycleLock)
            {
                return _isRetired;
            }
        }
    }

    internal Task DisposalTask => _disposalCompletion.Task;

    public static async Task<CodingWorkspaceToolSession> CreateAsync(
        string workspacePath,
        string languageServerCommand,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            throw new ArgumentException("工作区路径不能为空。", nameof(workspacePath));
        }

        if (string.IsNullOrWhiteSpace(languageServerCommand))
        {
            throw new ArgumentException("Roslyn Language Server 命令不能为空。", nameof(languageServerCommand));
        }

        string fullWorkspacePath = Path.GetFullPath(workspacePath);
        if (!Directory.Exists(fullWorkspacePath))
        {
            throw new DirectoryNotFoundException($"指定的代码工作区不存在: {fullWorkspacePath}");
        }

        RoslynAgentTools? roslynTools = null;
        try
        {
            var workspaceTools = new WorkspaceToolProvider
            {
                WorkspacePath = fullWorkspacePath,
            };
            var dotNetCliTools = new DotNetCliTools(fullWorkspacePath);
            IReadOnlyList<AITool> dotNetTools = dotNetCliTools.AsAITools();
            try
            {
                roslynTools = await RoslynAgentTools
                    .CreateAsync(fullWorkspacePath, languageServerCommand, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                roslynTools = RoslynAgentTools.CreateUnavailable(fullWorkspacePath);
            }

            IReadOnlyList<AITool> tools =
            [
                .. roslynTools.AsAITools(),
                .. workspaceTools.CreateDefaultTools(),
                .. dotNetTools,
            ];
            return new CodingWorkspaceToolSession(
                fullWorkspacePath,
                tools,
                roslynTools);
        }
        catch
        {
            if (roslynTools is not null)
            {
                await roslynTools.DisposeAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    internal CodingWorkspaceToolLease AcquireLease()
    {
        lock (_lifecycleLock)
        {
            if (_isRetired)
            {
                throw new ObjectDisposedException(nameof(CodingWorkspaceToolSession));
            }

            _leaseCount++;
        }

        return new CodingWorkspaceToolLease(this);
    }

    internal Task Retire()
    {
        lock (_lifecycleLock)
        {
            _isRetired = true;
            if (_leaseCount == 0)
            {
                _ = StartDisposalLocked();
            }

            return _disposalCompletion.Task;
        }
    }

    internal ValueTask ReleaseLeaseAsync()
    {
        Task? disposalTask = null;
        lock (_lifecycleLock)
        {
            if (_leaseCount <= 0)
            {
                throw new InvalidOperationException("工作区工具租约已全部释放。");
            }

            _leaseCount--;
            if (_isRetired && _leaseCount == 0)
            {
                disposalTask = StartDisposalLocked();
            }
        }

        return disposalTask is null ? default : new ValueTask(disposalTask);
    }

    public async ValueTask DisposeAsync()
    {
        Task disposalTask;
        lock (_lifecycleLock)
        {
            _isRetired = true;
            if (_leaseCount == 0)
            {
                _ = StartDisposalLocked();
            }

            disposalTask = _disposalCompletion.Task;
        }

        await disposalTask.ConfigureAwait(false);
    }

    private Task StartDisposalLocked()
    {
        if (!_isDisposalStarted)
        {
            _isDisposalStarted = true;
            _ = DisposeResourceAsync();
        }

        return _disposalCompletion.Task;
    }

    private async Task DisposeResourceAsync()
    {
        try
        {
            if (_asyncDisposable is not null)
            {
                await _asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }

            _disposalCompletion.TrySetResult();
        }
        catch (Exception ex)
        {
            _disposalCompletion.TrySetException(ex);
        }
    }
}
