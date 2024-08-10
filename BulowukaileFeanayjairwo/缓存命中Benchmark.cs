using BenchmarkDotNet.Attributes;

namespace benchmark;

// Copy from https://github.com/SeWZC

[BenchmarkCategory]
public class 缓存命中Benchmark
{
    // 4 GB
    public const int PoolCount = 1 << 30;
    private const int Count = 100_0000;

    private static int[] 待查找数组;
    private readonly int[] _乱序数组;
    private readonly int[] _顺序数组;

    static 缓存命中Benchmark()
    {
        待查找数组 = new int[PoolCount];
        for (var i = 0; i < PoolCount; i++)
        {
            待查找数组[i] = i;
        }
    }

    public 缓存命中Benchmark()
    {
        _乱序数组 = new int[Count];
        for (var i = 0; i < Count; i++)
        {
            // 这里的随机数是为了让数组乱序，但是性能测试上的随机数顺序将会极大影响测试结果
            _乱序数组[i] = Random.Shared.Next(0, PoolCount);
        }

        // 顺序数组只是为了和乱序数组对应，也做一次数组里面的取值
        _顺序数组 = new int[Count];
        for (var i = 0; i < Count; i++)
        {
            _顺序数组[i] = i;
        }
    }

    [Benchmark]
    public int 乱序数组读取()
    {
        var sum = 0;
        for (var i = 0; i < _乱序数组.Length; i++)
        {
            sum += 待查找数组[_乱序数组[i]];
        }

        return sum;
    }

    [Benchmark]
    public int 顺序数组读取()
    {
        var sum = 0;
        for (var i = 0; i < _顺序数组.Length; i++)
        {
            sum += 待查找数组[_顺序数组[i]];
        }

        return sum;
    }
}