using BenchmarkDotNet.Attributes;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Order;

namespace BulowukaileFeanayjairwo;

[SimpleJob(BenchmarkDotNet.Engines.RunStrategy.Throughput)]
public class Foo1
{
    [Benchmark]
    public void Foo()
    {

    }
}

[SimpleJob(BenchmarkDotNet.Engines.RunStrategy.Throughput)]
public class Foo2
{
    [Benchmark]
    public void Foo()
    {

    }
}