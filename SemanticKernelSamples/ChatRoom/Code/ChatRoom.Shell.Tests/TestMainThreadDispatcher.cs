using System;
using System.Threading.Tasks;

using AgentLib;

namespace ChatRoom.Shell.Tests;

/// <summary>
/// 测试用的 <see cref="IMainThreadDispatcher"/> 模拟实现。
/// 直接在当前线程同步执行，不进行线程调度。
/// </summary>
public sealed class TestMainThreadDispatcher : IMainThreadDispatcher
{
    /// <inheritdoc/>
    public Task InvokeAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return action();
    }

    /// <inheritdoc/>
    public Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return action();
    }

    /// <inheritdoc/>
    public bool CheckAccess() => true;
}
