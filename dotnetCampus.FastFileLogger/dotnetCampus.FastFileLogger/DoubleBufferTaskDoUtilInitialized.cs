#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

#pragma warning disable 164

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 可等待初始化之后才执行实际任务的双缓存工具
    /// <para>
    /// 在完成初始化之后需要调用 <see cref="OnInitialized"/> 方法
    /// </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DoubleBufferTaskDoUtilInitialized<T>
    {
        /// <summary>
        /// 初始化可等待初始化之后才执行实际任务的双缓存工具
        /// </summary>
        /// <param name="doTask">只有在 <see cref="OnInitialized"/> 方法被调用之后，才会执行的实际任务</param>
        public DoubleBufferTaskDoUtilInitialized(Func<List<T>, Task> doTask)
        {
            _doTask = doTask;
            _doubleBufferTask = new DoubleBufferTask<T>(DoInner);
        }

        /// <summary>
        /// 初始化完成之后调用，这个方法只能调用一次
        /// </summary>
        /// <exception cref="InvalidOperationException">如果调用多次，那么将抛出此异常</exception>
        public void OnInitialized()
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException($"禁止多次设置初始化完成");
            }

            // 这个变量设置无视线程安全，处理线程安全在 _waitForInitializationTask 字段
            _isInitialized = true;

            lock (Locker)
            {
                if (_waitForInitializationTask != null)
                {
                    // 如果不是空
                    // 那么设置任务完成
                    _waitForInitializationTask.SetResult(true);
                }
                else
                {
                    // 如果是空，那么 DoInner 还没进入，此时啥都不需要做
                }
            }
        }

        /// <summary>
        /// 加入任务
        /// </summary>
        /// <param name="data"></param>
        public void AddTask(T data)
        {
            _doubleBufferTask.AddTask(data);
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        public void Finish() => _doubleBufferTask.Finish();

        /// <summary>
        /// 等待完成任务，只有在调用 <see cref="Finish"/> 之后，所有任务执行完成才能完成
        /// </summary>
        /// <returns></returns>
        public Task WaitAllTaskFinish() => _doubleBufferTask.WaitAllTaskFinish();

        private object Locker => _doubleBufferTask;

        private readonly Func<List<T>, Task> _doTask;
        private readonly DoubleBufferTask<T> _doubleBufferTask;

        private bool _isInitialized;
        private TaskCompletionSource<bool>? _waitForInitializationTask;

        private async Task DoInner(List<T> dataList)
        {
            // 根据 DoubleBufferTask 的设计，这个方法只有一个线程进入
            FirstCheckInitialized: // 标签：第一个判断初始化方法
            if (!_isInitialized)
            {
                // 还没有初始化，等待一下
                // 如果此时还没有任务可以等待，那么创建一下任务
                lock (Locker)
                {
                    SecondCheckInitialized: // 标签：第二个判断初始化方法
                    if (!_isInitialized)
                    {
                        // 此时的值一定是空
                        Debug.Assert(_waitForInitializationTask == null);
                        _waitForInitializationTask = new TaskCompletionSource<bool>();
                    }
                }

                if (!_isInitialized)
                {
                    await _waitForInitializationTask!.Task;
                }
                else
                {
                    // 此时初始化方法被调用，因此不需要再调用等待
                    // 如果先进入 FirstCheckInitialized 标签的第一个判断初始化方法，此时 OnInitialized 没有被调用
                    // 因此进入分支
                    // 如果刚好此时 OnInitialized 方法进入，同时设置了 _isInitialized 是 true 值
                    // 如果此时的 OnInitialized 方法比 DoInner 先获得锁，那么将判断 _waitForInitializationTask 是空，啥都不做
                    // 然后 DoInner 在等待 OnInitialized 的 Locker 锁，进入锁之后，先通过 SecondCheckInitialized 标签的第二个判断初始化方法
                    // 这个判断是线程安全的，因此如果是 OnInitialized 已进入同时获取锁，那么此时在等待 Locker 锁之后一定拿到新的值
                    // 如果是 DoInner 先获得锁，那么此时也许 _isInitialized 不靠谱，但其实不依赖 _isInitialized 靠谱，因此 _isInitialized 只有一个状态，就是从 false 到 true 的值
                    // 此时如果判断 _isInitialized 是 true 的值，也就不需要再创建一个任务用来等待了
                    // 也就会最终进入此分支
                }

                // 只需要等待一次，然后可以释放内存
                _waitForInitializationTask = null;
            }

            await _doTask(dataList);
        }
    }
}