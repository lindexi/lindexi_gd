using System.Windows;
using AgentLib;

namespace PptxGenerator;

/// <summary>
/// WPF 实现的 <see cref="IMainThreadDispatcher"/>，用于将工作调度到 UI 线程。
/// </summary>
public sealed class WpfDispatcher : IMainThreadDispatcher
{
    /// <summary>
    /// 获取共享的调度器实例。
    /// </summary>
    public static readonly WpfDispatcher Instance = new();

    private WpfDispatcher()
    {
    }

    /// <inheritdoc />
    public Task InvokeAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var dispatcher = Application.Current.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            return action();
        }

        return dispatcher.InvokeAsync(action).Task.Unwrap();
    }

    /// <inheritdoc />
    public Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var dispatcher = Application.Current.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            return action();
        }

        return dispatcher.InvokeAsync(action).Task.Unwrap();
    }

    /// <inheritdoc />
    public bool CheckAccess()
    {
        return Application.Current.Dispatcher.CheckAccess();
    }
}
