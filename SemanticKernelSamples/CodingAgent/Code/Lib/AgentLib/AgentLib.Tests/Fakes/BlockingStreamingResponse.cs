using Microsoft.Extensions.AI;

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AgentLib.Tests.Fakes;

internal sealed class BlockingStreamingResponse
{
    private readonly TaskCompletionSource _startedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Started => _startedTaskCompletionSource.Task;

    public void Release()
    {
        _releaseTaskCompletionSource.TrySetResult();
    }

    public async IAsyncEnumerable<ChatResponseUpdate> CreateAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _startedTaskCompletionSource.TrySetResult();
        Task completedTask = await Task.WhenAny(_releaseTaskCompletionSource.Task, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
        await completedTask.ConfigureAwait(false);
        yield break;
    }
}
