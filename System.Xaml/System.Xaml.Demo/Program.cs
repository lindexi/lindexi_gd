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

            XamlObjectCreationFactory.RegisterCreator(() => new F1());
        }

        private static XamlSchemaContext XamlSchemaContext { set; get; } = new XamlSchemaContext();
    }
}
