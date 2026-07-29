using System.Windows;
using System.Windows.Threading;
using AgentLib;

namespace PptxGenerator;

/// <summary>
/// WPF 实现的 <see cref="IMainThreadDispatcher"/>，用于将工作调度到 UI 线程。
/// </summary>
public sealed class WpfDispatcher : IMainThreadDispatcher
{
    private static readonly Lazy<Task<Dispatcher>> FallbackDispatcherTask = new(CreateFallbackDispatcherAsync);
    private static Dispatcher? _fallbackDispatcher;
    private readonly bool _preferApplicationDispatcher;

    /// <summary>
    /// 获取共享的调度器实例。
    /// </summary>
    public static readonly WpfDispatcher Instance = new(preferApplicationDispatcher: true);

    /// <summary>
    /// 获取始终使用独立后台 STA 线程的调度器实例。
    /// </summary>
    public static readonly WpfDispatcher BackgroundInstance = new(preferApplicationDispatcher: false);

    private WpfDispatcher(bool preferApplicationDispatcher)
    {
        _preferApplicationDispatcher = preferApplicationDispatcher;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var dispatcher = await GetAvailableDispatcherAsync().ConfigureAwait(false);

        if (dispatcher.CheckAccess())
        {
            await action().ConfigureAwait(false);
            return;
        }

        await dispatcher.InvokeAsync(action).Task.Unwrap().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var dispatcher = await GetAvailableDispatcherAsync().ConfigureAwait(false);

        if (dispatcher.CheckAccess())
        {
            return await action().ConfigureAwait(false);
        }

        return await dispatcher.InvokeAsync(action).Task.Unwrap().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool CheckAccess()
    {
        if (GetApplicationDispatcher() is { } applicationDispatcher)
        {
            return applicationDispatcher.CheckAccess();
        }

        return Volatile.Read(ref _fallbackDispatcher)?.CheckAccess() == true;
    }

    private async Task<Dispatcher> GetAvailableDispatcherAsync()
    {
        if (GetApplicationDispatcher() is { } applicationDispatcher)
        {
            return applicationDispatcher;
        }

        return await FallbackDispatcherTask.Value.ConfigureAwait(false);
    }

    private Dispatcher? GetApplicationDispatcher()
    {
        if (!_preferApplicationDispatcher)
        {
            return null;
        }

        return Application.Current?.Dispatcher;
    }

    private static Task<Dispatcher> CreateFallbackDispatcherAsync()
    {
        var dispatcherReady = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            Volatile.Write(ref _fallbackDispatcher, dispatcher);
            dispatcherReady.SetResult(dispatcher);
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "PptxGenerator.WpfDispatcher",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return dispatcherReady.Task;
    }
}
