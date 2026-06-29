using System;
using System.Threading.Tasks;

using AgentLib;

using Avalonia.Threading;

namespace PptxGenerator;

/// <summary>
/// Avalonia 实现的 <see cref="IMainThreadDispatcher"/>，包装 <see cref="Dispatcher.UIThread"/>。
/// </summary>
public sealed class AvaloniaDispatcher : IMainThreadDispatcher
{
    /// <summary>
    /// 默认实例。
    /// </summary>
    public static readonly AvaloniaDispatcher Instance = new();

    private AvaloniaDispatcher()
    {
    }

    /// <inheritdoc />
    public Task InvokeAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Dispatcher.UIThread.InvokeAsync(async () => await action().ConfigureAwait(false));
    }

    /// <inheritdoc />
    public Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Dispatcher.UIThread.InvokeAsync(async () => await action().ConfigureAwait(false));
    }

    /// <inheritdoc />
    public bool CheckAccess()
    {
        return Dispatcher.UIThread.CheckAccess();
    }
}
