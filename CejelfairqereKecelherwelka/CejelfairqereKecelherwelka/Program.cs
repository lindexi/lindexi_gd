using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace CejelfairqereKecelherwelka
{
    /*
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19042.1200 (20H2/October2020Update)
Intel Core i7-9700K CPU 3.60GHz (Coffee Lake), 1 CPU, 8 logical and 8 physical cores
.NET SDK=6.0.100-preview.7.21379.14
  [Host]     : .NET 6.0.0 (6.0.21.37719), X64 RyuJIT
  DefaultJob : .NET 6.0.0 (6.0.21.37719), X64 RyuJIT

|      Method |     Mean |   Error |  StdDev |  Gen 0 |  Gen 1 | Allocated |
|------------ |---------:|--------:|--------:|-------:|-------:|----------:|
|   CopyByFor | 765.7 ns | 3.72 ns | 3.11 ns | 0.6409 | 0.0095 |      4 KB |
| CopyByArray | 260.6 ns | 3.39 ns | 3.17 ns | 0.6413 | 0.0095 |      4 KB |
| CopyByClone | 250.3 ns | 2.04 ns | 1.70 ns | 0.6390 | 0.0095 |      4 KB |
     */

    [MemoryDiagnoser]
    public class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Program>();
        }

        static Program()
        {
            TestData = new int[1000];
            for (int i = 0; i < 1000; i++)
            {
                TestData[i] = i;
            }
        }

        [Benchmark]
        public object CopyByFor()
        {
            var rawPacketData = TestData;
            var length = TestData.Length;

            var data = new int[length];
            for (int localIndex = 0, rawArrayIndex = 0; localIndex < data.Length; localIndex++, rawArrayIndex++)
            {
                data[localIndex] = rawPacketData[rawArrayIndex];
            }
            return data;
        }

        [Benchmark]
        public object CopyByArray()
        {
            var length = TestData.Length;
            var start = 0;

            var rawPacketData = TestData;
            var data = new int[length];
            Array.Copy(rawPacketData,start,data,0, length);
            return data;
        }

        [Benchmark]
        public object CopyByClone()
        {
            var data = (int[]) TestData.Clone();
            return data;
        }

        private static readonly int[] TestData;

       
    }
}
