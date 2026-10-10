using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentLib.Core;
using AgentLib.Logging;
using AgentLib.Model;

namespace CodingChatRoom.AvaloniaShell.Services;

internal interface ICodingChatSessionStore
{
    Task<IReadOnlyList<CopilotChatSessionSummary>> ListSessionsAsync(CancellationToken cancellationToken = default);

    async IAsyncEnumerable<CopilotChatSessionSummary> EnumerateSessionsAsync
    (
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default
    )
    {
        foreach (var summary in await ListSessionsAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return summary;
        }
    }

    Task<CopilotChatSession> LoadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task SaveSessionAsync(CopilotChatSession session, CancellationToken cancellationToken = default);
}