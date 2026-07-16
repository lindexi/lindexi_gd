using AgentLib.ChatRoom.Tools.Coding;
using Microsoft.Extensions.AI;

namespace AgentLib.ChatRoom.Services;

internal sealed class CodingWorkspaceToolSession : IAsyncDisposable
{
    private readonly IAsyncDisposable? _asyncDisposable;
    private int _isDisposed;

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
        Tools = tools;
    }

    public string WorkspacePath { get; }

    public IReadOnlyList<AITool> Tools { get; }

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

            IReadOnlyList<AITool> tools = [.. roslynTools.AsAITools(), .. dotNetTools];
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

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0 || _asyncDisposable is null)
        {
            return;
        }

        await _asyncDisposable.DisposeAsync().ConfigureAwait(false);
    }
}
