using AgentLib.Tools;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal sealed class CodingWorkspaceToolSession : IAsyncDisposable
{
    private static readonly string[] DefaultExcludedDirectoryNames =
    [
        ".git",
        ".vs",
        "artifacts",
        "bin",
        "obj",
        "TestResults",
    ];

    private readonly IAsyncDisposable? _asyncDisposable;
    private readonly object _lifecycleLock = new();
    private readonly TaskCompletionSource _disposalCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _leaseCount;
    private bool _isRetired;
    private bool _isDisposalStarted;

    internal CodingWorkspaceToolSession(
        string workspacePath,
        IReadOnlyList<ToolRegistration> registrations,
        IAsyncDisposable? asyncDisposable = null)
    {

        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            throw new ArgumentException("工作区路径不能为空。", nameof(workspacePath));
        }
        ArgumentNullException.ThrowIfNull(registrations);

        WorkspacePath = workspacePath;
        _asyncDisposable = asyncDisposable;
        ToolRegistrations = Array.AsReadOnly(registrations.ToArray());
        Tools = Array.AsReadOnly(ToolRegistrations.Select(registration => registration.Tool).ToArray());
        ToolRegistrationRegistry = new ToolRegistrationRegistry(ToolRegistrations);
    }

    public string WorkspacePath { get; }

    public IReadOnlyList<AITool> Tools { get; }

    public IReadOnlyList<ToolRegistration> ToolRegistrations { get; }

    public ToolRegistrationRegistry ToolRegistrationRegistry { get; }

    public static async Task<CodingWorkspaceToolSession> CreateAsync(
        string workspacePath,
        string languageServerCommand,
        IReadOnlyList<ICodingWorkspaceToolSource> additionalToolSources,
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
                AllowReadingOutsideWorkspace = true,
                WorkspacePath = fullWorkspacePath,
            };
            foreach (string directoryName in DefaultExcludedDirectoryNames)
            {
                workspaceTools.ExcludedDirectoryNames.Add(directoryName);
            }

            var dotNetCliTools = new DotNetCliTools(fullWorkspacePath);
            IReadOnlyList<ToolRegistration> dotNetTools = dotNetCliTools.AsToolRegistrations();
            var dotNetApiTools = new DotNetApiTools(fullWorkspacePath);
            var contentTools = new CodingWorkspaceContentTools(fullWorkspacePath);
            IReadOnlyList<ToolRegistration> additionalTools = additionalToolSources
                .SelectMany(source => source.CreateToolRegistrations(fullWorkspacePath))
                .ToArray();
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

            IReadOnlyList<ToolRegistration> registrations =
            [
                .. roslynTools.AsToolRegistrations(),
                .. workspaceTools.CreateDefaultToolRegistrations(),
                .. dotNetTools,
                .. dotNetApiTools.AsToolRegistrations(),
                .. contentTools.AsToolRegistrations(),
                .. additionalTools,
            ];
            return new CodingWorkspaceToolSession(
                fullWorkspacePath,
                registrations,
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
