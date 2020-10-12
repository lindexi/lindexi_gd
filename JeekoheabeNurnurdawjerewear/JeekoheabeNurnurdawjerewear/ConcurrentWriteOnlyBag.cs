using System;
using System.Collections.Generic;
using System.Threading;

namespace JeekoheabeNurnurdawjerewear
{
    public class ConcurrentWriteOnlyBag<T>
    {
        public ConcurrentWriteOnlyBag(int capacity)
        {
            Capacity = capacity;
            _buffer = new T[capacity];
        }

        public int Capacity { get; }

        public void Add(T value)
        {
            var currentIndex = Interlocked.Increment(ref _currentIndex);

            if (currentIndex > Capacity)
            {
                throw new ArgumentOutOfRangeException();
            }

            _buffer[currentIndex] = value;
        }

        /// <summary>
        /// 非线程安全
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<T> GetReadOnlyCollection() => _buffer;

        private readonly T[] _buffer;

        private int _currentIndex = -1;
    }
}