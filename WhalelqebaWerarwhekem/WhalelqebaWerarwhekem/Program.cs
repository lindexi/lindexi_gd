using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace WhalelqebaWerarwhekem
{
    class Program
    {
        private const int MaxCount = 100000000;

        static void Main(string[] args)
        {
            var maxCount = MaxCount;

            // 单线程的方法
            //var concurrentWriteOnlyList = new ConcurrentWriteOnlyList<int>();

            //var manualResetEventSlim = new ManualResetEventSlim(false);

            //for (int i = 0; i < 100; i++)
            //{
            //    var n = i;

            //    for (int j = 0; j < maxCount / 100; j++)
            //    {
            //        var value = maxCount / 100 * n + j;
            //        concurrentWriteOnlyList.Add(value);
            //    }

            //    //var thread = new Thread(() =>
            //    //{
            //    //    manualResetEventSlim.Wait();

            //    //    for (int j = 0; j < maxCount / 100; j++)
            //    //    {
            //    //        concurrentWriteOnlyList.Add(maxCount / 100 * n + j);
            //    //    }
            //    //});
            //    //thread.Start();
            //}

            //manualResetEventSlim.Set();

            //(int[] list, int currentIndex) = concurrentWriteOnlyList.GetList();

            //if (currentIndex != maxCount-1)
            //{

            //}

            //HashSet<int> hashSet = new HashSet<int>(maxCount);

            //for (int i = 0; i < currentIndex; i++)
            //{
            //    var n = list[i];

            //    if (hashSet.Add(n))
            //    {

            //    }
            //    else
            //    {

            //    }
            //}

            for (int i = 0; i < 1000; i++)
            {
                NakererecairfawduJakojuwho();
            }
        }

        private static void NakererecairfawduJakojuwho()
        {
            // 多线程
            var maxCount = MaxCount;

            var concurrentWriteOnlyList = new ConcurrentWriteOnlyList<int>();

            var manualResetEventSlim = new ManualResetEventSlim(false);
            var threadList = new Thread[10];
            maxCount /= 1000;

            for (int i = 0; i < threadList.Length; i++)
            {
                var n = i;

                var thread = new Thread(() =>
                {
                    manualResetEventSlim.Wait();

                    for (int j = 1; j < maxCount / threadList.Length + 1; j++)
                    {
                        concurrentWriteOnlyList.Add(maxCount / threadList.Length * n + j);
                    }
                });
                thread.Start();
                threadList[n] = thread;
            }

            manualResetEventSlim.Set();

            foreach (var thread in threadList)
            {
                thread.Join();
            }

            (int[] list, int currentIndex) = concurrentWriteOnlyList.GetList();

            if (currentIndex != maxCount - 1)
            {
            }

            HashSet<int> hashSet = new HashSet<int>(maxCount);

            for (int i = 0; i < currentIndex; i++)
            {
                var n = list[i];

                if (hashSet.Add(n))
                {
                }
                else
                {
                }
            }
        }
    }

    class ConcurrentWriteOnlyList<T>
    {
        private int _doingCount;

        public ConcurrentWriteOnlyList()
        {
            _list = new T[_capacity];
        }

        public ConcurrentWriteOnlyList(int capacity)
        {
            _capacity = capacity;
        }

        public void Add(T value)
        {
            var currentIndex = Interlocked.Increment(ref _currentIndex);
            Interlocked.Increment(ref _enterCount);
            var currentCapacity = _capacity;
            bool shouldResize = currentIndex >= currentCapacity;

            if (shouldResize)
            {
                Interlocked.Decrement(ref _enterCount);
                // 所有线程等待
                lock (_locker)
                {
                    // 如果上一个线程修改好了，那么忽略
                    // 如果上个线程修改了值还依然不够，那么继续修改
                    if (currentIndex >= _capacity)
                    {
                        var capacity = _capacity;
                        capacity *= 2;

                        capacity = Math.Max(capacity, currentIndex);

                        var newList = new T[capacity];

                        // 等待还在执行的线程全部执行完成
                        while (_doingCount > 0 || _enterCount > 0)
                        {
                            // 等待，加入的速度很快，因此让线程在这里执行
                        }

                        for (var i = 0; i < _list.Length; i++)
                        {
                            if (_list[i].Equals(0))
                            {

                            }
                        }

                        // 此时没有线程在修改 _list 的值
                        Array.Copy(_list, newList, _list.Length);
                        // 需要先设置 volatile 这样才能做到先设置 _list 的值
                        _list = newList;
                        // 这里无视线程安全问题
                        _capacity = capacity;
                    }
                }
            }
            else
            {
            }

            Interlocked.Increment(ref _doingCount);
            _list[currentIndex] = value;

            Interlocked.Decrement(ref _doingCount);

            if (!shouldResize)
            {
                Interlocked.Decrement(ref _enterCount);
            }
        }

        public (T[] _list, int _currentIndex) GetList() => (_list, _currentIndex);

        private readonly object _locker = new object();

        private int _currentIndex = -1;
        private volatile T[] _list;
        private volatile int _capacity = 16;
        private int _enterCount = 0;
    }
}
