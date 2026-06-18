using System;
using System.Threading.Tasks;

using AgentLib;

using Avalonia.Threading;

namespace ChatRoom.AvaloniaShell.Infrastructure;

/// <summary>
/// Avalonia 平台的 <see cref="IMainThreadDispatcher"/> 实现。
/// 包装 <see cref="Dispatcher.UIThread"/>，将操作调度到 UI 线程。
/// </summary>
public sealed class AvaloniaMainThreadDispatcher : IMainThreadDispatcher
{
    /// <inheritdoc/>
    public Task InvokeAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Dispatcher.UIThread.InvokeAsync(action);
    }

    /// <inheritdoc/>
    public Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Dispatcher.UIThread.InvokeAsync(action);
    }

    /// <inheritdoc/>
    public bool CheckAccess()
    {
        return Dispatcher.UIThread.CheckAccess();
    }
}
