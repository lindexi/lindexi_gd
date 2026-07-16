using AgentLib.ChatRoom.Services;

using Microsoft.Extensions.AI;

namespace AgentLib.ChatRoom.Tools.Coding;

internal sealed class CodingWorkspaceRoleTool : IChatRoomRoleTool, IChatRoomWorkspaceAwareTool
{
    private readonly string _languageServerCommand;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private CodingWorkspaceToolSession? _session;
    private int _isDisposed;

    internal CodingWorkspaceRoleTool(string languageServerCommand)
    {
        if (string.IsNullOrWhiteSpace(languageServerCommand))
        {
            throw new ArgumentException("Roslyn Language Server 命令不能为空。", nameof(languageServerCommand));
        }

        _languageServerCommand = languageServerCommand;
    }

    public IReadOnlyList<AITool> AITools => _session?.Tools ?? [];

    public async Task SetWorkspacePathAsync(string? workspacePath, CancellationToken cancellationToken)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                throw new ObjectDisposedException(nameof(CodingWorkspaceRoleTool));
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
