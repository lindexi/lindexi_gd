using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Test>();
            Console.Read();
        }
    }

    public class Test
    {
        [Benchmark()]
        public void CalcGeometry()
        {
            WpfInk.Test.CalcGeometryAndBoundsWithTransform();
        }

        [Benchmark(Baseline = true)]
        public void CalcGeometryOld()
        {
            WpfInkOld.Test.CalcGeometryAndBoundsWithTransform();
        }
    }
}
