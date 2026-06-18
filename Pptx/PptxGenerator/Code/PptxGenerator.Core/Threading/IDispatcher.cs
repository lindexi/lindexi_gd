using System;
using System.Threading.Tasks;

namespace PptxGenerator;

/// <summary>
/// UI 线程调度器抽象接口，解耦 UI 框架特定的 Dispatcher 类型。
/// 各 UI 框架（WPF、Avalonia）通过实现此接口来封装各自的调度器。
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// 在 UI 线程上异步执行指定操作。
    /// </summary>
    /// <param name="action">要在 UI 线程上执行的操作。</param>
    void InvokeAsync(Action action);

    /// <summary>
    /// 在 UI 线程上异步执行指定操作并返回结果。
    /// </summary>
    /// <typeparam name="T">返回值类型。</typeparam>
    /// <param name="func">要在 UI 线程上执行的函数。</param>
    /// <returns>操作结果。</returns>
    T InvokeAsync<T>(Func<T> func);
}