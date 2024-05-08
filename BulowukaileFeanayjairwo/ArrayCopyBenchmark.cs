using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;

namespace BulowukaileFeanayjairwo;

[SimpleJob(BenchmarkDotNet.Engines.RunStrategy.Throughput)]
public unsafe class ArrayCopyBenchmark
{
    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(GetArgument))]
    public void CopyByFor(int[] source, int[] dest)
    {
        for (int i = 0; i < source.Length; i++)
        {
            dest[i] = source[i];
        }
    }

    [Benchmark()]
    [ArgumentsSource(nameof(GetArgument))]
    public void MemcpyLibc(int[] source, int[] dest)
    {
        if (!IsLinux)
        {
            return;
        }

        fixed (int* sourcePtr = source)
        fixed (int* destinationPtr = dest)
        {
            Memcpy(new IntPtr(destinationPtr), new IntPtr(sourcePtr), source.Length * sizeof(int));
        }
    }

    [DllImport("libc.so.6", EntryPoint = "memcpy")]
    static extern void Memcpy(IntPtr dest, IntPtr src, IntPtr count);

    private static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    [Benchmark]
    [ArgumentsSource(nameof(GetArgument))]
    public void CopyBlockUnaligned(int[] source, int[] dest)
    {
        fixed (int* sourcePtr = source)
        fixed (int* destinationPtr = dest)
        {
            Unsafe.CopyBlockUnaligned(destinationPtr, sourcePtr, (uint) source.Length * sizeof(int));
        }
    }

    public IEnumerable<object[]> GetArgument()
    {
        foreach (var length in new int[] { 10, 20, 100 })
        {
            yield return CreateArrayInner(length);
        }

        object[] CreateArrayInner(int length)
        {
            var (source, dest) = CreateArray(length);
            return [source, dest];
        }
    }

    private (int[] source, int[] dest) CreateArray(int length)
    {
        var a = new int[length];
        var b = new int[length];

        for (int i = 0; i < a.Length; i++)
        {
            a[i] = i;
        }

        return (a, b);
    }
}