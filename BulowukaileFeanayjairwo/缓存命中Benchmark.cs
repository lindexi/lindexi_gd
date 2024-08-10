using BenchmarkDotNet.Attributes;

namespace benchmark;

// Copy from https://github.com/SeWZC

[BenchmarkCategory]
public class 缓存命中Benchmark
{
    // 4 GB
    public const int PoolCount = 1 << 30;
    private const int Count = 100_0000;

    public static int[] 待查找数组;
    public int[] 乱序数组;
    public int[] 顺序数组;

    static 缓存命中Benchmark()
    {
        待查找数组 = new int[PoolCount];
        for (var i = 0; i < PoolCount; i++)
            待查找数组[i] = i;
    }

    public 缓存命中Benchmark()
    {
        乱序数组 = new int[Count];
        for (var i = 0; i < Count; i++)
            乱序数组[i] = Random.Shared.Next(0, PoolCount);
        顺序数组 = new int[Count];
        for (var i = 0; i < Count; i++)
            顺序数组[i] = i;
    }

    [Benchmark]
    public int 乱序数组读取()
    {
        var sum = 0;
        for (var i = 0; i < 乱序数组.Length; i++)
            sum += 待查找数组[乱序数组[i]];

        return sum;
    }

    [Benchmark]
    public int 顺序数组读取()
    {
        var sum = 0;
        for (var i = 0; i < 顺序数组.Length; i++)
            sum += 待查找数组[顺序数组[i]];

        return sum;
    }
}