using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace HurnabahearwhawJehearhefena
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }

    public class AddAndRemoveAppDomainShutdownMonitorTest
    {
        /*
        BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.685 (2004/?/20H1)
        Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
        .NET Core SDK=5.0.100
          [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
          DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT


|             Method |              argList |     Mean |    Error |   StdDev | Ratio | RatioSD |
|------------------- |--------------------- |---------:|---------:|---------:|------:|--------:|
| 'AddAndRemove Old' | Syste(...)ence] [55] | 679.7 us | 10.42 us |  8.70 us |  1.00 |    0.00 |
|                    |                      |          |          |          |       |         |
| 'AddAndRemove New' |  Syst(...)er]] [113] | 667.7 us | 11.07 us | 10.35 us |     ? |       ? |
         */
        public IEnumerable<List<WeakReference>> GetOldAppDomainShutdownMonitorArgList()
        {
            var argList = new List<WeakReference>();
            for (int i = 0; i < 10000; i++)
            {
                var fakeAppDomainShutdownListener = new FakeAppDomainShutdownListener();
                var weakReference = new WeakReference(fakeAppDomainShutdownListener);
                argList.Add(weakReference);
            }

            yield return argList;
        }

        public IEnumerable<List<WeakReference<IAppDomainShutdownListener>>> GetNewAppDomainShutdownMonitorArgList()
        {
            var argList = new List<WeakReference<IAppDomainShutdownListener>>();
            for (int i = 0; i < 10000; i++)
            {
                var fakeAppDomainShutdownListener = new FakeAppDomainShutdownListener();
                var weakReference = new WeakReference<IAppDomainShutdownListener>(fakeAppDomainShutdownListener);
                argList.Add(weakReference);
            }

            yield return argList;
        }

        [Benchmark(Baseline = true, Description = "AddAndRemove Old")]
        [ArgumentsSource(nameof(GetOldAppDomainShutdownMonitorArgList))]
        public void TestOldAppDomainShutdownMonitor(List<WeakReference> argList)
        {
            var appDomainShutdownMonitor = new AppDomainShutdownMonitor();

            foreach (var weakReference in argList)
            {
                appDomainShutdownMonitor.Add(weakReference);
                appDomainShutdownMonitor.Remove(weakReference);
            }
        }

        [Benchmark(Description = "AddAndRemove New")]
        [ArgumentsSource(nameof(GetNewAppDomainShutdownMonitorArgList))]
        public void TestNewAppDomainShutdownMonitor(List<WeakReference<IAppDomainShutdownListener>> argList)
        {
            var appDomainShutdownMonitor = new AppDomainShutdownMonitorNew();

            foreach (var weakReference in argList)
            {
                appDomainShutdownMonitor.Add(weakReference);
                appDomainShutdownMonitor.Remove(weakReference);
            }
        }

        //[Benchmark(Description = "AddAndRemove New")]
        //[ArgumentsSource(nameof(GetNewAppDomainShutdownMonitorArgList))]
        //public void TestNewAppDomainShutdownMonitor(List<IAppDomainShutdownListener> argList)
        //{
        //    var appDomainShutdownMonitorWithLinkedList = new AppDomainShutdownMonitorWithLinkedList();

        //    foreach (var appDomainShutdownListener in argList)
        //    {
        //        var token = appDomainShutdownMonitorWithLinkedList.Register(appDomainShutdownListener);
        //        appDomainShutdownMonitorWithLinkedList.Remove(token);
        //    }
        //}
    }

    public class AddBeforeRemoveAppDomainShutdownMonitorTest
    {
        /*
        BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.685 (2004/?/20H1)
        Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
        .NET Core SDK=5.0.100
          [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
          DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT


|                Method |              argList |     Mean |     Error |    StdDev | Ratio | RatioSD |
|---------------------- |--------------------- |---------:|----------:|----------:|------:|--------:|
| 'AddBeforeRemove Old' | Syste(...)ence] [55] | 1.414 ms | 0.0174 ms | 0.0154 ms |  1.00 |    0.00 |
|                       |                      |          |           |           |       |         |
| 'AddBeforeRemove New' |  Syst(...)er]] [113] | 1.197 ms | 0.0097 ms | 0.0081 ms |     ? |       ? |
         */

        public IEnumerable<List<WeakReference>> GetOldAppDomainShutdownMonitorArgList()
        {
            var argList = new List<WeakReference>();
            for (int i = 0; i < 10000; i++)
            {
                var fakeAppDomainShutdownListener = new FakeAppDomainShutdownListener();
                var weakReference = new WeakReference(fakeAppDomainShutdownListener);
                argList.Add(weakReference);
            }

            yield return argList;
        }

        public IEnumerable<List<WeakReference<IAppDomainShutdownListener>>> GetNewAppDomainShutdownMonitorArgList()
        {
            var argList = new List<WeakReference<IAppDomainShutdownListener>>();
            for (int i = 0; i < 10000; i++)
            {
                var fakeAppDomainShutdownListener = new FakeAppDomainShutdownListener();
                var weakReference = new WeakReference<IAppDomainShutdownListener>(fakeAppDomainShutdownListener);
                argList.Add(weakReference);
            }

            yield return argList;
        }

        [Benchmark(Baseline = true, Description = "AddBeforeRemove Old")]
        [ArgumentsSource(nameof(GetOldAppDomainShutdownMonitorArgList))]
        public void TestOldAppDomainShutdownMonitor(List<WeakReference> argList)
        {
            var appDomainShutdownMonitor = new AppDomainShutdownMonitor();

            foreach (var weakReference in argList)
            {
                appDomainShutdownMonitor.Add(weakReference);
            }

            foreach (var weakReference in argList)
            {
                appDomainShutdownMonitor.Remove(weakReference);
            }
        }

        [Benchmark(Description = "AddBeforeRemove New")]
        [ArgumentsSource(nameof(GetNewAppDomainShutdownMonitorArgList))]
        public void TestNewAppDomainShutdownMonitor(List<WeakReference<IAppDomainShutdownListener>> argList)
        {
            var appDomainShutdownMonitor = new AppDomainShutdownMonitorNew();

            foreach (var weakReference in argList)
            {
                appDomainShutdownMonitor.Add(weakReference);
            }

            foreach (var weakReference in argList)
            {
                appDomainShutdownMonitor.Remove(weakReference);
            }
        }

        //[Benchmark(Description = "AddBeforeRemove New")]
        //[ArgumentsSource(nameof(GetNewAppDomainShutdownMonitorArgList))]
        //public void TestNewAppDomainShutdownMonitor(List<IAppDomainShutdownListener> argList)
        //{
        //    var appDomainShutdownMonitorWithLinkedList = new AppDomainShutdownMonitorWithLinkedList();
        //    var tokenList = new List<AppDomainShutdownMonitorToken>(argList.Count);

        //    foreach (var appDomainShutdownListener in argList)
        //    {
        //        var token = appDomainShutdownMonitorWithLinkedList.Register(appDomainShutdownListener);
        //        tokenList.Add(token);
        //    }

        //    foreach (var appDomainShutdownMonitorToken in tokenList)
        //    {
        //        appDomainShutdownMonitorWithLinkedList.Remove(appDomainShutdownMonitorToken);
        //    }
        //}
    }

    public class AddOnlyAppDomainShutdownMonitorTest
    {
        /*
        BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.685 (2004/?/20H1)
        Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
        .NET Core SDK=5.0.100
          [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
          DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT


|        Method |     Mean |     Error |    StdDev |   Median | Ratio | RatioSD |
|-------------- |---------:|----------:|----------:|---------:|------:|--------:|
| 'AddOnly Old' | 2.886 ms | 0.0574 ms | 0.1160 ms | 2.821 ms |  1.00 |    0.00 |
| 'AddOnly New' | 2.880 ms | 0.0639 ms | 0.1865 ms | 2.827 ms |  1.00 |    0.09 |
         */

        [Benchmark(Baseline = true, Description = "AddOnly Old")]
        public void TestOldAppDomainShutdownMonitor()
        {
            var fakeAppDomainShutdownListener = new FakeAppDomainShutdownListener();
            var appDomainShutdownMonitor = new AppDomainShutdownMonitor();

            for (int i = 0; i < 10000; i++)
            {
                var weakReference = new WeakReference(fakeAppDomainShutdownListener);
                appDomainShutdownMonitor.Add(weakReference);
            }
        }

        [Benchmark(Description = "AddOnly New")]
        public void TestNewAppDomainShutdownMonitor()
        {
            var fakeAppDomainShutdownListener = new FakeAppDomainShutdownListener();
            var appDomainShutdownMonitor = new AppDomainShutdownMonitorNew();

            for (int i = 0; i < 10000; i++)
            {
                var weakReference = new WeakReference<IAppDomainShutdownListener>(fakeAppDomainShutdownListener);
                appDomainShutdownMonitor.Add(weakReference);
            }
        }

        //[Benchmark(Description = "AddOnly New")]
        //public void TestNewAppDomainShutdownMonitor()
        //{
        //    var fakeAppDomainShutdownListener = new FakeAppDomainShutdownListener();
        //    var appDomainShutdownMonitorWithLinkedList = new AppDomainShutdownMonitorWithLinkedList();
        //    for (int i = 0; i < 10000; i++)
        //    {
        //        appDomainShutdownMonitorWithLinkedList.Register(fakeAppDomainShutdownListener);
        //    }
        //}
    }

    readonly struct AppDomainShutdownMonitorToken
    {
        public AppDomainShutdownMonitorToken(LinkedListNode<WeakReference<IAppDomainShutdownListener>> node)
        {
            Node = node;
        }

        public LinkedListNode<WeakReference<IAppDomainShutdownListener>> Node { get; }
    }

    internal class AppDomainShutdownMonitor
    {
        private readonly Dictionary<WeakReference, WeakReference> _dictionary =
            new Dictionary<WeakReference, WeakReference>();

        private static bool _shuttingDown;

        public void Add(WeakReference listener)
        {
            Debug.Assert(listener.Target != null);
            Debug.Assert(listener.Target is IAppDomainShutdownListener);

            lock (_dictionary)
            {
                if (!_shuttingDown)
                {
                    _dictionary.Add(listener, listener);
                }
            }
        }

        public void Remove(WeakReference listener)
        {
            Debug.Assert(listener.Target == null || listener.Target is IAppDomainShutdownListener);

            lock (_dictionary)
            {
                if (!_shuttingDown)
                {
                    _dictionary.Remove(listener);
                }
            }
        }
    }

    class AppDomainShutdownMonitorWithLinkedList
    {
        public AppDomainShutdownMonitorToken Register(IAppDomainShutdownListener listener)
        {
            var weakReference = new WeakReference<IAppDomainShutdownListener>(listener);

            lock (Locker)
            {
                var node = ListenerList.AddLast(weakReference);
                var appDomainShutdownMonitorToken = new AppDomainShutdownMonitorToken(node);
                return appDomainShutdownMonitorToken;
            }
        }

        public void Remove(AppDomainShutdownMonitorToken token)
        {
            lock (Locker)
            {
                ListenerList.Remove(token.Node);
            }
        }

        private object Locker => ListenerList;

        private LinkedList<WeakReference<IAppDomainShutdownListener>> ListenerList { get; } =
            new LinkedList<WeakReference<IAppDomainShutdownListener>>();
    }

    public interface IAppDomainShutdownListener
    {
        void NotifyShutdown();
    }

    class FakeAppDomainShutdownListener : IAppDomainShutdownListener
    {
        public void NotifyShutdown()
        {
        }
    }


    internal class AppDomainShutdownMonitorNew
    {
        //public AppDomainShutdownMonitorNew()
        //{
        //    AppDomain.CurrentDomain.DomainUnload += OnShutdown;
        //    AppDomain.CurrentDomain.ProcessExit += OnShutdown;
        //}

        public void Add(WeakReference<IAppDomainShutdownListener> listener)
        {
            Debug.Assert(listener.TryGetTarget(out _));

            lock (_hashSet)
            {
                if (!_shuttingDown)
                {
                    _hashSet.Add(listener);
                }
            }
        }

        public void Remove(WeakReference<IAppDomainShutdownListener> listener)
        {
            lock (_hashSet)
            {
                if (!_shuttingDown)
                {
                    _hashSet.Remove(listener);
                }
            }
        }

        private void OnShutdown(object sender, EventArgs e)
        {
            lock (_hashSet)
            {
                // Setting this to true prevents Add and Remove from modifying the list. This
                // way we call out without holding a lock (which would be bad)
                _shuttingDown = true;
            }

            foreach (var weakReference in _hashSet)
            {
                if (weakReference.TryGetTarget(out var listener))
                {
                    listener.NotifyShutdown();
                }
            }
        }

        private readonly HashSet<WeakReference<IAppDomainShutdownListener>> _hashSet =
            new HashSet<WeakReference<IAppDomainShutdownListener>>();

        private bool _shuttingDown;
    }
}