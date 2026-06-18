using System;
using System.Threading.Tasks;
using System.Windows;

namespace PptxGenerator;

/// <summary>
/// WPF 实现的 <see cref="IDispatcher"/>，包装 <see cref="Application.Current.Dispatcher"/>。
/// </summary>
public sealed class WpfDispatcher : IDispatcher
{
    /// <summary>
    /// 默认实例，使用 <see cref="Application.Current.Dispatcher"/>。
    /// </summary>
    public static readonly WpfDispatcher Instance = new();

    private WpfDispatcher()
    {
    }

    /// <inheritdoc />
    public void InvokeAsync(Action action)
    {
        Application.Current.Dispatcher.InvokeAsync(action);
    }

    /// <inheritdoc />
    public T InvokeAsync<T>(Func<T> func)
    {
        return Application.Current.Dispatcher.Invoke(func);
    }
}