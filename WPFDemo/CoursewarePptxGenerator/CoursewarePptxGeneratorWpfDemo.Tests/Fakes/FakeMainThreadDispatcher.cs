using AgentLib;

namespace CoursewarePptxGeneratorWpfDemo.Tests.Fakes;

internal sealed class FakeMainThreadDispatcher : IMainThreadDispatcher
{
    public async Task InvokeAsync(Func<Task> action)
    {
        await action().ConfigureAwait(false);
    }

    public async Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        return await action().ConfigureAwait(false);
    }

    public bool CheckAccess() => true;
}
