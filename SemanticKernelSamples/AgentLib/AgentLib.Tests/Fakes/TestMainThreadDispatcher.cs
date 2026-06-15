using System;
using System.Threading.Tasks;

namespace AgentLib.Tests.Fakes;

/// <summary>
/// 测试用的主线程调度器桩。用于验证线程调度逻辑是否正确工作。
/// </summary>
internal sealed class TestMainThreadDispatcher : IMainThreadDispatcher
{
    private bool _isMainThread;

    /// <summary>
    /// 使用指定的主线程标识创建调度器桩。
    /// </summary>
    /// <param name="isMainThread">模拟当前是否在主线程上。</param>
    public TestMainThreadDispatcher(bool isMainThread)
    {
        _isMainThread = isMainThread;
        InvokeCount = 0;
    }

    /// <summary>
    /// 获取 <see cref="InvokeAsync"/> 被调用的次数。
    /// </summary>
    public int InvokeCount { get; private set; }

    /// <summary>
    /// 模拟调度到主线程执行。执行回调期间 <see cref="CheckAccess"/> 将返回 <see langword="true"/>，
    /// 回调执行完毕后恢复原始状态。
    /// </summary>
    /// <param name="action">要执行的操作。</param>
    public async Task InvokeAsync(Func<Task> action)
    {
        InvokeCount++;

        bool originalIsMainThread = _isMainThread;
        _isMainThread = true;
        try
        {
            await action();
        }
        finally
        {
            _isMainThread = originalIsMainThread;
        }
    }

    /// <summary>
    /// 返回当前是否在主线程上。
    /// </summary>
    public bool CheckAccess()
    {
        return _isMainThread;
    }
}