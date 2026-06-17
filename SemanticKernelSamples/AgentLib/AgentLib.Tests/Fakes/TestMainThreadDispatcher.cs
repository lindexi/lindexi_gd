using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.Tests.Fakes;

/// <summary>
/// 测试用的主线程调度器桩。支持线程身份追踪、自定义 <see cref="SynchronizationContext"/>、
/// 线程切换历史记录和严格断言模式，用于验证线程调度逻辑是否正确工作。
/// </summary>
internal sealed class TestMainThreadDispatcher : IMainThreadDispatcher
{
    private bool _isMainThread;
    private readonly SingleThreadSynchronizationContext _synchronizationContext;
    private SynchronizationContext? _previousSynchronizationContext;

    /// <summary>
    /// 使用指定的主线程标识创建调度器桩。
    /// </summary>
    /// <param name="isMainThread">模拟当前是否在主线程上。</param>
    /// <param name="strictMode">启用后，<see cref="CheckAccess"/> 在非主线程上调用时将抛出异常。</param>
    public TestMainThreadDispatcher(bool isMainThread, bool strictMode = false)
    {
        _isMainThread = isMainThread;
        StrictMode = strictMode;
        InvokeCount = 0;
        MainThread = Thread.CurrentThread;
        MainThreadId = Environment.CurrentManagedThreadId;
        InvokeHistory = [];
        SynchronizationContextSnapshots = [];
        _synchronizationContext = new SingleThreadSynchronizationContext(MainThread);
    }

    /// <summary>
    /// 获取 <see cref="InvokeAsync"/> 被调用的次数。
    /// </summary>
    public int InvokeCount { get; private set; }

    /// <summary>
    /// 获取调度器创建时捕获的主线程实例。
    /// </summary>
    public Thread MainThread { get; }

    /// <summary>
    /// 获取调度器创建时捕获的主线程托管 ID。
    /// </summary>
    public int MainThreadId { get; }

    /// <summary>
    /// 获取是否启用严格模式。严格模式下 <see cref="CheckAccess"/> 在非主线程调用时抛出异常。
    /// </summary>
    public bool StrictMode { get; }

    /// <summary>
    /// 获取每次 <see cref="InvokeAsync"/> 调用的线程切换历史记录。
    /// </summary>
    public List<InvokeRecord> InvokeHistory { get; }

    /// <summary>
    /// 获取每次回调执行期间 <see cref="SynchronizationContext.Current"/> 的快照记录。
    /// </summary>
    public List<SynchronizationContextSnapshot> SynchronizationContextSnapshots { get; }

    /// <summary>
    /// 模拟调度到主线程执行。执行回调期间 <see cref="CheckAccess"/> 将返回 <see langword="true"/>，
    /// <see cref="SynchronizationContext.Current"/> 将设置为自定义的单线程上下文，
    /// 回调执行完毕后恢复原始状态。
    /// </summary>
    /// <param name="action">要执行的操作。</param>
    public async Task InvokeAsync(Func<Task> action)
    {
        InvokeCount++;

        int callerThreadId = Environment.CurrentManagedThreadId;
        Thread callerThread = Thread.CurrentThread;
        bool originalIsMainThread = _isMainThread;
        _previousSynchronizationContext = SynchronizationContext.Current;

        _isMainThread = true;
        SynchronizationContext.SetSynchronizationContext(_synchronizationContext);

        int callbackThreadId = Environment.CurrentManagedThreadId;
        Thread callbackThread = Thread.CurrentThread;
        bool checkAccessDuringCallback = CheckAccess();

        try
        {
            await action().ConfigureAwait(false);

            SynchronizationContextSnapshots.Add(new SynchronizationContextSnapshot(
                ContextType: SynchronizationContext.Current?.GetType().Name ?? "null",
                IsCustomContext: SynchronizationContext.Current is SingleThreadSynchronizationContext));
        }
        finally
        {
            _isMainThread = originalIsMainThread;
            SynchronizationContext.SetSynchronizationContext(_previousSynchronizationContext);
            InvokeHistory.Add(new InvokeRecord(
                CallerThreadId: callerThreadId,
                CallerThread: callerThread,
                CallbackThreadId: callbackThreadId,
                CallbackThread: callbackThread,
                CheckAccessDuringCallback: checkAccessDuringCallback));
        }
    }

    /// <summary>
    /// 模拟调度到主线程执行，并返回结果。行为与 <see cref="InvokeAsync(Func{Task})"/> 一致。
    /// </summary>
    /// <typeparam name="T">返回值类型。</typeparam>
    /// <param name="action">要在主线程上执行的异步操作，返回 <typeparamref name="T"/> 类型的值。</param>
    /// <returns>表示调度操作完成的 <see cref="Task{TResult}"/>，包含执行结果。</returns>
    public async Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        InvokeCount++;

        int callerThreadId = Environment.CurrentManagedThreadId;
        Thread callerThread = Thread.CurrentThread;
        bool originalIsMainThread = _isMainThread;
        _previousSynchronizationContext = SynchronizationContext.Current;

        _isMainThread = true;
        SynchronizationContext.SetSynchronizationContext(_synchronizationContext);

        int callbackThreadId = Environment.CurrentManagedThreadId;
        Thread callbackThread = Thread.CurrentThread;
        bool checkAccessDuringCallback = CheckAccess();

        try
        {
            T result = await action().ConfigureAwait(false);

            SynchronizationContextSnapshots.Add(new SynchronizationContextSnapshot(
                ContextType: SynchronizationContext.Current?.GetType().Name ?? "null",
                IsCustomContext: SynchronizationContext.Current is SingleThreadSynchronizationContext));

            return result;
        }
        finally
        {
            _isMainThread = originalIsMainThread;
            SynchronizationContext.SetSynchronizationContext(_previousSynchronizationContext);
            InvokeHistory.Add(new InvokeRecord(
                CallerThreadId: callerThreadId,
                CallerThread: callerThread,
                CallbackThreadId: callbackThreadId,
                CallbackThread: callbackThread,
                CheckAccessDuringCallback: checkAccessDuringCallback));
        }
    }

    /// <summary>
    /// 返回当前是否在主线程上。严格模式下，在非主线程调用时将抛出 <see cref="InvalidOperationException"/>。
    /// </summary>
    public bool CheckAccess()
    {
        if (_isMainThread)
        {
            return true;
        }

        if (StrictMode)
        {
            throw new InvalidOperationException(
                $"CheckAccess 在非主线程上被调用。当前线程 ID: {Environment.CurrentManagedThreadId}，" +
                $"期望主线程 ID: {MainThreadId}。");
        }

        return false;
    }

    /// <summary>
    /// 记录每次 <see cref="InvokeAsync"/> 调用的线程切换信息。
    /// </summary>
    /// <param name="CallerThreadId">调用方线程托管 ID。</param>
    /// <param name="CallerThread">调用方线程实例。</param>
    /// <param name="CallbackThreadId">回调开始执行时的线程托管 ID。</param>
    /// <param name="CallbackThread">回调开始执行时的线程实例。</param>
    /// <param name="CheckAccessDuringCallback">回调执行期间 <see cref="CheckAccess"/> 的返回值。</param>
    public sealed record InvokeRecord(
        int CallerThreadId,
        Thread CallerThread,
        int CallbackThreadId,
        Thread CallbackThread,
        bool CheckAccessDuringCallback);

    /// <summary>
    /// 记录回调执行期间 <see cref="SynchronizationContext.Current"/> 的状态。
    /// </summary>
    public sealed record SynchronizationContextSnapshot(
        string ContextType,
        bool IsCustomContext);
}