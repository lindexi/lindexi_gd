namespace AgentLib.Coding;

internal sealed class CodingWorkspaceToolCandidate : IAsyncDisposable
{
    private CodingWorkspaceToolSession? _session;
    private Action? _onHandled;
    private int _state;

    internal CodingWorkspaceToolCandidate(CodingWorkspaceToolSession? session, Action? onHandled = null)
    {
        _session = session;
        _onHandled = onHandled;
        WorkspacePath = session?.WorkspacePath;
    }

    internal string? WorkspacePath { get; }

    internal CodingWorkspaceToolSession? TakeSession()
    {
        if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
        {
            throw new InvalidOperationException("工作区候选已经发布或释放。");
        }

        return Interlocked.Exchange(ref _session, null);
    }

    internal void CompleteHandling()
    {
        int oldState = Interlocked.Exchange(ref _state, 2);
        if (oldState == 2)
        {
            return;
        }

        Interlocked.Exchange(ref _onHandled, null)?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        int oldState = Interlocked.Exchange(ref _state, 2);
        if (oldState == 2)
        {
            return;
        }

        try
        {
            CodingWorkspaceToolSession? session = Interlocked.Exchange(ref _session, null);
            if (session is not null)
            {
                await session.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _onHandled, null)?.Invoke();
        }
    }
}
