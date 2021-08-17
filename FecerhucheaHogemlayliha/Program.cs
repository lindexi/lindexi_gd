using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FecerhucheaHogemlayliha
{
    class Program
    {
        static async Task Main(string[] args)
        {
            for (int i = 0; i < 1; i++)
            {
                _ = Task.Run(Call);
            }
            Console.Read();
            Console.Read();

            async Task Call()
            {
                using (new CallStack())
                {
                    CallStackContext.Current["argument"] = Guid.NewGuid();
                    _ = FooAsync();
                    _ = BarAsync();
                    _ = BazAsync();
                    await Task.Delay(1);
                    for (int i = 0; i < 1; i++)
                    {
                        _ = Task.Run(() => Trace());
                    }
                }
            }
        }
        static Task FooAsync() => Task.Run(() => Trace());
        static Task BarAsync() => Task.Run(() => Trace());
        static Task BazAsync() => Task.Run(() => Trace());
        static void Trace([CallerMemberName] string methodName = null)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var traceId = CallStackContext.Current?.TraceId;

            var argument = CallStackContext.Current?["argument"];
            for (int i = 0; i < 100; i++)
            {
                CallStackContext.Current[i.ToString()] = methodName;
                var n = CallStackContext.Current?[i.ToString()];
            }

            Console.WriteLine($"Thread: {threadId}; TraceId: {traceId}; Method: {methodName}; Argument:{argument}");
        }
    }

    public class CallStackContext : Dictionary<string, object> //, ILogicalThreadAffinative
    {
        internal static readonly AsyncLocal<CallStackContext> _contextAccessor = new AsyncLocal<CallStackContext>();
        private static int _traceId = 0;
        public static CallStackContext Current => _contextAccessor.Value;
        public long TraceId { get; } = Interlocked.Increment(ref _traceId);
    }

    public class CallStack : IDisposable
    {
        public CallStack() => CallStackContext._contextAccessor.Value = new CallStackContext();
        public void Dispose() => CallStackContext._contextAccessor.Value = null;
    }
}
