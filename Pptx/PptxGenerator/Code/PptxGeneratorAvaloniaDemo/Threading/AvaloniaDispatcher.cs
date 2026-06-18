using System;
using Avalonia.Threading;

namespace PptxGenerator;

/// <summary>
/// Avalonia 实现的 <see cref="IDispatcher"/>，包装 <see cref="Dispatcher.UIThread"/>。
/// </summary>
public sealed class AvaloniaDispatcher : IDispatcher
{
    /// <summary>
    /// 默认实例。
    /// </summary>
    public static readonly AvaloniaDispatcher Instance = new();

    private AvaloniaDispatcher()
    {
    }

    /// <inheritdoc />
    public void InvokeAsync(Action action)
    {
        Dispatcher.UIThread.InvokeAsync(action);
    }

    /// <inheritdoc />
    public T InvokeAsync<T>(Func<T> func)
    {
        return Dispatcher.UIThread.Invoke(func);
    }
}