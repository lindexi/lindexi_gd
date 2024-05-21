using Microsoft.UI.Dispatching;

namespace UnoFileDownloader.Utils
{
    public record DispatcherQueueProvider(DispatcherQueue Dispatcher) : IDispatcherQueueProvider
    {
    }

    /// <summary>
    /// 用于让可获取注入的逻辑获取到主线程调度
    /// </summary>
    public interface IDispatcherQueueProvider
    {
        Microsoft.UI.Dispatching.DispatcherQueue Dispatcher { get; }
    }
}
