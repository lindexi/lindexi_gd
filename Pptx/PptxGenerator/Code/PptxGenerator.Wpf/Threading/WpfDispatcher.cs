using System.Windows;
using System.Windows.Threading;
using AgentLib;

namespace PptxGenerator;

/// <summary>
/// WPF 实现的 <see cref="IMainThreadDispatcher"/>，用于将工作调度到 UI 线程。
/// </summary>
public sealed class WpfDispatcher : IMainThreadDispatcher
{
    private static readonly Lazy<Dispatcher> FallbackDispatcher = new(CreateFallbackDispatcher);
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
    public Task InvokeAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var dispatcher = GetAvailableDispatcher();

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

        var dispatcher = GetAvailableDispatcher();

        if (dispatcher.CheckAccess())
        {
            return action();
        }

        return dispatcher.InvokeAsync(action).Task.Unwrap();
    }

    /// <inheritdoc />
    public bool CheckAccess()
    {
        return GetAvailableDispatcher().CheckAccess();
    }

    private Dispatcher GetAvailableDispatcher()
    {
        if (_preferApplicationDispatcher)
        {
            var applicationDispatcher = Application.Current?.Dispatcher;
            if (applicationDispatcher is { HasShutdownStarted: false, HasShutdownFinished: false })
            {
                return applicationDispatcher;
            }
        }

        return FallbackDispatcher.Value;
    }

    private static Dispatcher CreateFallbackDispatcher()
    {
        var dispatcherReady = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            dispatcherReady.SetResult(dispatcher);
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "PptxGenerator.WpfDispatcher",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return dispatcherReady.Task.GetAwaiter().GetResult();
    }
}
