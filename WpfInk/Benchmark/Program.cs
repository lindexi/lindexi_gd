using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            WpfInk.Test.CalcGeometryAndBoundsWithTransform();
            BenchmarkRunner.Run<Test>();
            Console.Read();
        }
    }

    public class Test
    {
        [Benchmark()]
        [ArgumentsSource(nameof(WpfInkSource))]
        public void CalcGeometry(WpfInk.Context context)
        {
            WpfInk.Test.CalcGeometryAndBoundsWithTransform(context);
        }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(WpfInkOldSource))]
        public void CalcGeometryOld(WpfInkOld.Context context)
        {
            WpfInkOld.Test.CalcGeometryAndBoundsWithTransform(context);
        }

        public IEnumerable<WpfInk.Context> WpfInkSource()
        {
            var context = WpfInk.Test.GetContext(PointList.GetPointList());
            yield return context;
        }

        public IEnumerable<WpfInkOld.Context> WpfInkOldSource()
        {
            var context = WpfInkOld.Test.GetContext(PointList.GetPointList());
            yield return context;
        }
    }
}
