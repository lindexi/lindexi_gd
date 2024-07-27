namespace BulowukaileFeanayjairwo;

/// <summary>
/// 线性同余法 简单固定的随机数生成器
/// </summary>
class StaticRandom
{
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

        return (double)_seed / m;
    }
}