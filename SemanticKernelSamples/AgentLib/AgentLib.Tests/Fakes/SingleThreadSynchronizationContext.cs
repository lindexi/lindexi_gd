using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace AgentLib.Tests.Fakes;

/// <summary>
/// 模拟单线程同步上下文，用于测试场景。所有 Post 和 Send 操作都会在指定线程上执行。
/// </summary>
internal sealed class SingleThreadSynchronizationContext : SynchronizationContext
{
    private readonly Thread _ownerThread;
    private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _workItems = new();
    private readonly AutoResetEvent _workAvailable = new(false);
    private volatile bool _isRunning;
    private Thread? _processingThread;

    /// <summary>
    /// 创建绑定到指定线程的同步上下文。
    /// </summary>
    /// <param name="ownerThread">拥有此上下文的线程。</param>
    public SingleThreadSynchronizationContext(Thread ownerThread)
    {
        _ownerThread = ownerThread;
    }

    /// <summary>
    /// 获取当前是否正在处理消息队列。
    /// </summary>
    public bool IsProcessing => _isRunning;

    /// <summary>
    /// 获取处理消息队列的线程。
    /// </summary>
    public Thread? ProcessingThread => _processingThread;

    /// <summary>
    /// 异步将工作项排入队列。
    /// </summary>
    /// <param name="callback">要执行的回调。</param>
    /// <param name="state">传递给回调的状态。</param>
    public override void Post(SendOrPostCallback callback, object? state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _workItems.Enqueue((callback, state));
        _workAvailable.Set();
    }

    /// <summary>
    /// 同步发送工作项并等待完成。
    /// </summary>
    /// <param name="callback">要执行的回调。</param>
    /// <param name="state">传递给回调的状态。</param>
    public override void Send(SendOrPostCallback callback, object? state)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (Thread.CurrentThread == _ownerThread)
        {
            callback(state);
            return;
        }

        var completed = new ManualResetEventSlim(false);
        Exception? exception = null;

        _workItems.Enqueue((
            _ =>
            {
                try
                {
                    callback(state);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    completed.Set();
                }
            },
            (object?)null));

        _workAvailable.Set();
        completed.Wait();

        if (exception is not null)
        {
            throw new AggregateException(exception);
        }
    }

    /// <summary>
    /// 创建此同步上下文的副本。
    /// </summary>
    /// <returns>新的同步上下文实例。</returns>
    public override SynchronizationContext CreateCopy()
    {
        return new SingleThreadSynchronizationContext(_ownerThread);
    }

    /// <summary>
    /// 开始处理消息队列。此方法应在拥有线程上调用。
    /// </summary>
    public void StartProcessing()
    {
        if (Thread.CurrentThread != _ownerThread)
        {
            throw new InvalidOperationException("必须在拥有线程上启动消息处理。");
        }

        _isRunning = true;
        _processingThread = _ownerThread;

        while (_isRunning)
        {
            if (_workItems.TryDequeue(out var workItem))
            {
                try
                {
                    workItem.Callback(workItem.State);
                }
                catch (Exception)
                {
                    // 在测试场景中，异常应该被捕获并传播给调用者
                    throw;
                }
            }
            else
            {
                _workAvailable.WaitOne(10);
            }
        }
    }

    /// <summary>
    /// 停止处理消息队列。
    /// </summary>
    public void StopProcessing()
    {
        _isRunning = false;
        _workAvailable.Set();
    }

    /// <summary>
    /// 处理队列中的所有待处理工作项。
    /// </summary>
    public void ProcessAll()
    {
        while (_workItems.TryDequeue(out var workItem))
        {
            workItem.Callback(workItem.State);
        }
    }

    /// <summary>
    /// 获取待处理的工作项数量。
    /// </summary>
    public int PendingWorkCount => _workItems.Count;
}
