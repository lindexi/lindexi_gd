using System.Windows.Threading;
using AgentLib;

namespace DeepSeekWpf.Infrastructure;

public sealed class WpfMainThreadDispatcher : IMainThreadDispatcher
{
    private readonly Dispatcher _dispatcher;

    public WpfMainThreadDispatcher(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public bool CheckAccess()
    {
        return _dispatcher.CheckAccess();
    }

    public Task InvokeAsync(Func<Task> action)
    {
        return _dispatcher.InvokeAsync(action).Task.Unwrap();
    }

    public Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        return _dispatcher.InvokeAsync(action).Task.Unwrap();
    }
}
