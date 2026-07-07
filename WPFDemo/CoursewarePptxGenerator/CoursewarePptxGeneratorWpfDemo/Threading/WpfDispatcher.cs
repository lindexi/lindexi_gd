using System.Windows;
using AgentLib;

namespace CoursewarePptxGeneratorWpfDemo.Threading;

/// <summary>
/// Dispatches work to the WPF UI thread.
/// </summary>
public sealed class WpfDispatcher : IMainThreadDispatcher
{
    /// <summary>
    /// Gets the shared dispatcher instance.
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
