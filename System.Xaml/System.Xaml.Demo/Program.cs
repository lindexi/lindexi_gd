using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xaml.Schema;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace System.Xaml.Demo
{
    public class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Program>();
        }
        /*
         * BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.746 (2004/?/20H1)
           Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
           .NET Core SDK=5.0.101
           [Host]     : .NET Core 5.0.1 (CoreCLR 5.0.120.57516, CoreFX 5.0.120.57516), X64 RyuJIT
           DefaultJob : .NET Core 5.0.1 (CoreCLR 5.0.120.57516, CoreFX 5.0.120.57516), X64 RyuJIT
           
           
           |                                                    Method |     Mean |    Error |  StdDev | Ratio | RatioSD |
           |---------------------------------------------------------- |---------:|---------:|--------:|------:|--------:|
           |                        CreateInstanceInXamlTypeInvokerOld | 595.1 ns | 10.20 ns | 8.51 ns |  1.00 |    0.00 |
           |    CreateInstanceWhichRegisterInXamlObjectCreationFactory | 575.1 ns |  7.04 ns | 5.88 ns |  0.97 |    0.02 |
           | CreateInstanceWhichNotRegisterInXamlObjectCreationFactory | 575.6 ns |  6.45 ns | 6.03 ns |  0.97 |    0.01 |
         */

        [Benchmark(Baseline = true)]
        public object CreateInstanceInXamlTypeInvokerOld()
        {
            var xamlTypeInvokerOld = new XamlTypeInvokerOld(new XamlType(typeof(F1), XamlSchemaContext));
            return xamlTypeInvokerOld.CreateInstance(Array.Empty<object>());
        }

        [Benchmark]
        public object CreateInstanceWhichRegisterInXamlObjectCreationFactory()
        {
            var xamlTypeInvoker = new XamlTypeInvoker(new XamlType(typeof(F1), XamlSchemaContext));
            return xamlTypeInvoker.CreateInstance(Array.Empty<object>());
        }

        [Benchmark]
        public object CreateInstanceWhichNotRegisterInXamlObjectCreationFactory()
        {
            var xamlTypeInvoker = new XamlTypeInvoker(new XamlType(typeof(F2), XamlSchemaContext));
            return xamlTypeInvoker.CreateInstance(Array.Empty<object>());
        }

        [GlobalSetup]
        public void Init()
        {
            XamlSchemaContext = new XamlSchemaContext();

            //XamlObjectCreationFactory.RegisterCreator(() => new F1());
        }

        private static XamlSchemaContext XamlSchemaContext { set; get; } = new XamlSchemaContext();
    }
}
