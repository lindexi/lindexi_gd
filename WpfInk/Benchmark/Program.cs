using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var erasingStrokeTest = new ErasingStrokeTest();
            erasingStrokeTest.EraseTestOnce(erasingStrokeTest.WpfInkSource().First());
            //WpfInk.Test.CalcGeometryAndBoundsWithTransform();
            BenchmarkRunner.Run<ErasingStrokeTest>();
            Console.Read();
        }
    }

    public class ErasingStrokeTest
    {
        [Benchmark()]
        [ArgumentsSource(nameof(WpfInkSource))]
        public void EraseTestOnce(WpfInkErasingStroke.ErasingStrokeTest.TextContext textContext)
        {
            textContext.EraseTest();
        }

        [Benchmark()]
        [ArgumentsSource(nameof(WpfInkSource))]
        public void EraseTest10Times(WpfInkErasingStroke.ErasingStrokeTest.TextContext textContext)
        {
            for (int i = 0; i < 10; i++)
            {
                textContext.EraseTest();
            }
        }

        [Benchmark()]
        [ArgumentsSource(nameof(WpfInkOldSource))]
        public void EraseTextOldOnce(WpfInkOld.ErasingStrokeTest.TextContext textContext)
        {
            textContext.EraseTest();
        }

        [Benchmark()]
        [ArgumentsSource(nameof(WpfInkOldSource))]
        public void EraseTextOld10Times(WpfInkOld.ErasingStrokeTest.TextContext textContext)
        {
            for (int i = 0; i < 10; i++)
            {
                textContext.EraseTest();
            }
        }

        public IEnumerable<WpfInkErasingStroke.ErasingStrokeTest.TextContext> WpfInkSource()
        {
            var erasingStrokeTest = new WpfInkErasingStroke.ErasingStrokeTest();
            yield return erasingStrokeTest.GetErasingStroke(PointList.GetPointList());
        }

        public IEnumerable<WpfInkOld.ErasingStrokeTest.TextContext> WpfInkOldSource()
        {
            var erasingStrokeTest = new WpfInkOld.ErasingStrokeTest();
            yield return erasingStrokeTest.GetErasingStroke(PointList.GetPointList());
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