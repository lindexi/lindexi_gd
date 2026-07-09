using System.Windows;

namespace CoursewarePptxGeneratorWpfDemo.Threading;

/// <summary>
/// Dispatches ViewModel state updates through the WPF dispatcher.
/// </summary>
public sealed class WpfViewModelDispatcher : IViewModelDispatcher
{
    /// <summary>
    /// Gets the shared WPF ViewModel dispatcher instance.
    /// </summary>
    public static WpfViewModelDispatcher Instance { get; } = new();

    private WpfViewModelDispatcher()
    {
    }

    /// <inheritdoc />
    public async Task InvokeAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            await action().ConfigureAwait(true);
            return;
        }

        var task = await dispatcher.InvokeAsync(action).Task.ConfigureAwait(false);
        await task.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        await dispatcher.InvokeAsync(action).Task.ConfigureAwait(false);
    }
}
