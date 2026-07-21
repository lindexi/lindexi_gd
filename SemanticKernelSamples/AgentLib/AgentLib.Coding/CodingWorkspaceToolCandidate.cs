namespace AgentLib.Coding;

internal sealed class CodingWorkspaceToolCandidate : IAsyncDisposable
{
    private CodingWorkspaceToolSession? _session;
    private int _isConsumed;

    internal CodingWorkspaceToolCandidate(CodingWorkspaceToolSession? session)
    {
        _session = session;
        WorkspacePath = session?.WorkspacePath;
    }

    internal string? WorkspacePath { get; }

    internal CodingWorkspaceToolSession? TakeSession()
    {
        if (Interlocked.Exchange(ref _isConsumed, 1) != 0)
        {
            throw new InvalidOperationException("工作区候选已经发布或释放。");
        }

        return Interlocked.Exchange(ref _session, null);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isConsumed, 1) != 0)
        {
            return;
        }

        CodingWorkspaceToolSession? session = Interlocked.Exchange(ref _session, null);
        if (session is not null)
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }
    }
}
