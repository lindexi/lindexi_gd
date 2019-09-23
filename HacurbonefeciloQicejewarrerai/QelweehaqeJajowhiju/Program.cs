using System;
using System.Diagnostics;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using HacurbonefeciloQicejewarrerai;

namespace QelweehaqeJajowhiju
{
    public class Program
    {
        static void Main(string[] args)
        {
            Process.Start("HacurbonefeciloQicejewarrerai.exe");
        }

        [Benchmark]
        public bool FindExistByProcessName()
        {
            var name = "HacurbonefeciloQicejewarrerai";
            return Process.GetProcessesByName(name).Length > 0;
        }

        [Benchmark]
        public bool FindNotExistByProcessName()
        {
            return Process.GetProcessesByName("不存在的进程").Length > 0;
        }

        [Benchmark]
        public bool FindExistByMutex()
        {
            return Mutex.TryOpenExisting(Const.Lock, out var result);
        }

        [Benchmark]
        public bool FindNotExistByMutex()
        {
            return Mutex.TryOpenExisting("不存在的进程", out var result);
        }
    }
}