using System;
using System.Threading.Tasks;

using AgentLib;

using Avalonia.Threading;

namespace CodingChatRoom.AvaloniaShell.Infrastructure;

/// <summary>
/// 将 AgentLib 的主线程操作调度到 Avalonia UI 线程。
/// </summary>
public sealed class AvaloniaMainThreadDispatcher : IMainThreadDispatcher
{
    /// <inheritdoc />
    public Task InvokeAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Dispatcher.UIThread.InvokeAsync(action);
    }

    /// <inheritdoc />
    public Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Dispatcher.UIThread.InvokeAsync(action);
    }

    /// <inheritdoc />
    public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();
}
