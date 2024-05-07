using BenchmarkDotNet.Attributes;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Order;

namespace BulowukaileFeanayjairwo;

[SimpleJob(BenchmarkDotNet.Engines.RunStrategy.Throughput)]
public class ArrayBenchmark
{
    [Params(10, 100, 1_000, 10_000, 100_000, 1_000_000)]
    public int ArraySize { get; set; }

    [Benchmark(Baseline = true)]
    public int[] NewArray() => new int[ArraySize];

    [Benchmark]
    public int[] GCZeroInitialized() => GC.AllocateArray<int>(ArraySize);

    [Benchmark]
    public int[] GCZeroUninitialized() => GC.AllocateUninitializedArray<int>(ArraySize);


}

[SimpleJob(BenchmarkDotNet.Engines.RunStrategy.Throughput)]
public class Foo2
{
    [Benchmark]
    public void Foo()
    {

    }
}

public class LinearCongruentialGenerator
{
    private long _seed;
    private const long a = 48271;
    private const long m = 2147483647;
    private const long q = m / a;
    private const long r = m % a;

    public LinearCongruentialGenerator(int seed)
    {
        if (seed <= 0 || seed == int.MaxValue)
        {
            throw new Exception("Seed must be a positive integer less than int.MaxValue");
        }

        _seed = seed;
    }

    public double NextDouble()
    {
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
}
