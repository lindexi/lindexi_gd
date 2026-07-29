using PptxGenerator;

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
    public async Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        await WpfDispatcher.Instance.InvokeAsync(() =>
        {
            action();
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }
}
