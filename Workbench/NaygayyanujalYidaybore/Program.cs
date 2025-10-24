using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

using System;
using System.Collections.Generic;
using System.Text;

using BenchmarkDotNet.Jobs;

namespace NaygayyanujalYidaybore;

[SimpleJob(RunStrategy.ColdStart, RuntimeMoniker.Net90, launchCount: 1, warmupCount: 5, iterationCount: 500, invocationCount: 500, id: "FastAndDirtyJob")]
[SimpleJob(RunStrategy.ColdStart, RuntimeMoniker.Net10_0, launchCount: 1, warmupCount: 5, iterationCount: 500, invocationCount: 500, id: "FastAndDirtyJob")]
public class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<Program>();
    }

    [GlobalSetup]
    public void Setup()
    {
        var count = 20000;
        for (int i = 0; i < count; i++)
        {
            _list.Add(new P(i));
        }
    }

    private readonly List<P> _list = new List<P>();

    public const string TheText = "\n";

    private IReadOnlyList<P> GetList() => _list;

    [Benchmark]
    public int BenchmarkMethod1()
    {
        var list = GetList();

        if (list.Count == 0)
        {
            return 0;
        }

        int sum = 0;

        foreach (var p in list)
        {
            sum += p.Count;
            sum += P.TheTextLength;
        }

        if (sum > 0)
        {
            sum -= P.TheTextLength;
        }

        return sum;
    }
}

record P(int Count)
{
    public static int TheTextLength => Program.TheText.Length;
}
