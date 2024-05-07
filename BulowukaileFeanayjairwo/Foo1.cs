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