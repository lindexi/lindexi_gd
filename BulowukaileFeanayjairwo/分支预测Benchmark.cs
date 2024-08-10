using BenchmarkDotNet.Attributes;

namespace benchmark;

// Copy from https://github.com/SeWZC
// 但是这样的预测是有问题的，测试逻辑构建不咋合理
[BenchmarkCategory]
public class 分支预测Benchmark
{
    private const int Count = 100_0000;
    public int[] 乱序数组;
    public int[] 顺序数组;

    public 分支预测Benchmark()
    {
        顺序数组 = Enumerable.Range(0, Count).ToArray();
        乱序数组 = new int[Count];
        顺序数组.CopyTo(乱序数组, 0);
        Random.Shared.Shuffle(乱序数组);
    }

    [Benchmark]
    public int 乱序数组判断()
    {
        var sum = 0;
        for (var i = 0; i < 乱序数组.Length; i++)
        {
            if (乱序数组[i] > Count / 2)
            {
                sum++;
            }
        }

        return sum;
    }

    [Benchmark]
    public int 顺序数组判断()
    {
        var sum = 0;
        for (var i = 0; i < 顺序数组.Length; i++)
        {
            if (顺序数组[i] > Count / 2)
            {
                sum++;
            }
        }

        return sum;
    }
}