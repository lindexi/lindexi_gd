using System;
using System.Threading.Tasks;

namespace AgentLib;

/// <summary>
/// 主线程调度器接口。用于将操作调度回 UI 主线程执行，避免跨线程修改 UI 绑定集合。
/// </summary>
public interface IMainThreadDispatcher
{
    /// <summary>
    /// 将指定的异步操作调度到主线程执行。
    /// </summary>
    /// <param name="action">要在主线程上执行的异步操作。</param>
    /// <returns>表示调度操作完成的 <see cref="Task"/>。</returns>
    Task InvokeAsync(Func<Task> action);

    /// <summary>
    /// 判断当前执行上下文是否在主线程上。
    /// 设计为方法而非属性，因为线程访问检查可能是重量级操作。
    /// </summary>
    /// <returns>如果当前在主线程上返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    bool CheckAccess();
}
