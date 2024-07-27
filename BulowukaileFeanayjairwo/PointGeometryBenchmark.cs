using BenchmarkDotNet.Attributes;

using System.Drawing;

namespace BulowukaileFeanayjairwo;

[SimpleJob(BenchmarkDotNet.Engines.RunStrategy.Throughput)]
public class PointGeometryBenchmark
{
    [Benchmark()]
    [ArgumentsSource(nameof(GetArgument))]
    public void Test(Point[] source, double[] result)
    {
        for (int i = 1; i < source.Length - 1; i++)
        {
            var a = source[i - 1];
            var b = source[i];
            var c = source[i + 1];

            var abx = b.X - a.X;
            var aby = b.Y - a.Y;

            var acx = c.X - a.X;
            var acy = c.Y - a.Y;

            var cross = abx * acy - aby * acx;
            var abs = Math.Abs(cross);

            var acl = Math.Sqrt(acx * acx + acy * acy);

            result[i] = abs / acl;
        }
    }

    public IEnumerable<object[]> GetArgument()
    {
        foreach (var length in new int[] { 1000, 10000 })
        {
            yield return CreateArrayInner(length);
        }

        object[] CreateArrayInner(int length)
        {
            var source = new Point[length];
            var staticRandom = new StaticRandom();

            for (int i = 0; i < length; i++)
            {
                source[i] = new Point(staticRandom.GenerateLinearCongruential(), staticRandom.GenerateLinearCongruential());
            }

            var result = new double[length];

            return new object[] { source, result };
        }
    }
}

public readonly record struct Point(double X, double Y);
