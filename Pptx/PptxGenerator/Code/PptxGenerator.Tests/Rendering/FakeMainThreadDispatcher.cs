using AgentLib;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// 测试用主线程调度器 Fake 实现，直接在当前线程执行操作。
/// </summary>
public sealed class FakeMainThreadDispatcher : IMainThreadDispatcher
{
    /// <inheritdoc />
    public async Task InvokeAsync(Func<Task> action)
    {
        await action().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        return await action().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool CheckAccess() => true;
}
