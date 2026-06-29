using System;
using System.Threading.Tasks;
using System.Windows;

using AgentLib;

namespace PptxGenerator;

/// <summary>
/// WPF 实现的 <see cref="IMainThreadDispatcher"/>，包装 <see cref="Application.Current.Dispatcher"/>。
/// </summary>
public sealed class WpfDispatcher : IMainThreadDispatcher
{
    /// <summary>
    /// 默认实例，使用 <see cref="Application.Current.Dispatcher"/>。
    /// </summary>
    public static readonly WpfDispatcher Instance = new();

    private WpfDispatcher()
    {
    }

    /// <inheritdoc />
    public Task InvokeAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Application.Current.Dispatcher.InvokeAsync(async () => await action().ConfigureAwait(false)).Task.Unwrap();
    }

    /// <inheritdoc />
    public Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Application.Current.Dispatcher.InvokeAsync(async () => await action().ConfigureAwait(false)).Task.Unwrap();
    }

    /// <inheritdoc />
    public bool CheckAccess()
    {
        return Application.Current.Dispatcher.CheckAccess();
    }
}
