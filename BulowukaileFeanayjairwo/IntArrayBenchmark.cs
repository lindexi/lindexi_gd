using BenchmarkDotNet.Attributes;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Order;

namespace BulowukaileFeanayjairwo;

[SimpleJob(BenchmarkDotNet.Engines.RunStrategy.Throughput)]
public class IntArrayBenchmark
{
    [Params(10, 100, 1_000, 10_000, 100_000, 1_000_000, 1_000_000_000)]
    public int ArraySize { get; set; }

    [Benchmark(Baseline = true)]
    public int[] NewArray() => new int[ArraySize];

    [Benchmark]
    public int[] GCZeroInitialized() => GC.AllocateArray<int>(ArraySize);

    [Benchmark]
    public int[] GCZeroUninitialized() => GC.AllocateUninitializedArray<int>(ArraySize);

    [Benchmark]
    public int[] NewArrayWithRandomVisit()
    {
        // 测试随机访问性能
        var buffer = new int[ArraySize];
        var count = (int) Math.Sqrt(ArraySize);
        for (int i = 0; i < count; i++)
        {
            var index = (int) GenerateLinearCongruential() % buffer.Length;
            buffer[index] = i;
        }

        return buffer;
    }

    [Benchmark]
    public long NewArrayWithOrdinalVisit()
    {
        // 测试随机访问性能
        var buffer = new int[ArraySize];
        for (int i = 0; i < ArraySize; i++)
        {
            buffer[i] = i;
        }

        long sum = 0;
        for (int i = 0; i < ArraySize; i++)
        {
            sum += buffer[i];
        }

        return sum;
    }

    #region 线性同余法 
    // 提供固定的且相同的简单的值

    private long _seed = 1596779460; // 随便写的数

    public double GenerateLinearCongruential()
    {
        const long a = 48271;
        const long m = int.MaxValue;
        const long q = m / a;
        const long r = m % a;
        long hi = _seed / q;
        long lo = _seed % q;
        long test = a * lo - r * hi;

        if (test > 0)
        {
            _seed = test;

        }
        else
        {
            _seed = test + m;
        }

        return (double) _seed / m;
    }

    #endregion
}
