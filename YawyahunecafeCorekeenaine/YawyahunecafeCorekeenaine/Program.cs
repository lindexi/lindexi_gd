using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace YawyahunecafeCorekeenaine
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Foo>();
        }
    }

    public class Foo
    {
        [GlobalSetup]
        public void Init()
        {
            _concurrentDictionary = new ConcurrentDictionary<int, object>();
            _concurrentDictionary.TryAdd(-1, GetObject());
        }



        [Benchmark]
        public object GetOrAddExistWithClosed()
        {
            object o = null;
            for (int i = 0; i < Count; i++)
            {
                o = GetObject();

                _concurrentDictionary.GetOrAdd(-1, _ => o);
            }

            return o;
        }

        [Benchmark]
        public object GetOrAddExistWithValue()
        {
            object o = GetObject();
            for (int i = 0; i < Count; i++)
            {
                o = _concurrentDictionary.GetOrAdd(-1, (_, value) => value, o);
            }

            return o;
        }

        [Benchmark]
        public object GetOrAddNotExistWithClosed()
        {
            object o = GetObject();
            for (int i = 0; i < Count; i++)
            {
                _concurrentDictionary.GetOrAdd(i, _ => o);
            }

            return o;
        }

        [Benchmark]
        public object GetOrAddNotExistWithValue()
        {
            object o = GetObject();
            for (int i = 0; i < Count; i++)
            {
                _concurrentDictionary.GetOrAdd(i, (_, value) => value, o);
            }

            return o;
        }

        [Benchmark]
        public object GetOrAddExistWithoutClosed()
        {
            object o = null;
            for (int i = 0; i < Count; i++)
            {
                o = _concurrentDictionary.GetOrAdd(-1, _ => GetObject());
            }

            return o;
        }

        [Benchmark]
        public object GetOrAddNotExistWithoutClosed()
        {
            object o = null;
            for (int i = 0; i < Count; i++)
            {
                o = _concurrentDictionary.GetOrAdd(i, _ => GetObject());
            }

            return o;
        }

        [Benchmark]
        public object TryGetValueExist()
        {
            object o = null;
            for (int i = 0; i < Count; i++)
            {
                if (_concurrentDictionary.TryGetValue(-1, out var value))
                {

                }
                else
                {
                    o = GetObject();

                    _concurrentDictionary.TryAdd(-1, o);
                }
            }

            return o;
        }

        [Benchmark]
        public object TryGetValueNotExist()
        {
            object o = null;
            for (int i = 0; i < Count; i++)
            {
                if (_concurrentDictionary.TryGetValue(i, out var value))
                {
                }
                else
                {
                    o = GetObject();

                    _concurrentDictionary.TryAdd(i, o);
                }
            }

            return o;
        }

        private const int Count = 100;

        private object GetObject() => _closedObject;

        private readonly object _closedObject = new object();

        private ConcurrentDictionary<int, object> _concurrentDictionary;
    }
}
