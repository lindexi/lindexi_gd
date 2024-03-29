﻿using System;
using System.Collections.Generic;
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

<<<<<<< HEAD

|      Method | start | length |      Mean |     Error |    StdDev |  Gen 0 |  Gen 1 | Allocated |
|------------ |------ |------- |----------:|----------:|----------:|-------:|-------:|----------:|
|   CopyByFor |     0 |     10 | 10.940 ns | 0.0908 ns | 0.0850 ns | 0.0102 |      - |      64 B |
| CopyByArray |     0 |     10 |  9.861 ns | 0.0784 ns | 0.0655 ns | 0.0102 |      - |      64 B |
|   CopyByFor |     0 |     20 | 18.666 ns | 0.2216 ns | 0.2073 ns | 0.0166 |      - |     104 B |
| CopyByArray |     0 |     20 | 13.494 ns | 0.1908 ns | 0.1785 ns | 0.0166 |      - |     104 B |
|   CopyByFor |     0 |    100 | 88.846 ns | 0.5168 ns | 0.4834 ns | 0.0675 |      - |     424 B |
| CopyByArray |     0 |    100 | 32.594 ns | 0.4785 ns | 0.4476 ns | 0.0675 | 0.0001 |     424 B |
|   CopyByFor |    10 |     10 | 10.966 ns | 0.1215 ns | 0.1136 ns | 0.0102 |      - |      64 B |
| CopyByArray |    10 |     10 | 10.383 ns | 0.1375 ns | 0.1286 ns | 0.0102 |      - |      64 B |
|   CopyByFor |    10 |     20 | 18.866 ns | 0.1972 ns | 0.1844 ns | 0.0166 |      - |     104 B |
| CopyByArray |    10 |     20 | 13.001 ns | 0.1484 ns | 0.1239 ns | 0.0166 |      - |     104 B |
|   CopyByFor |    10 |    100 | 88.808 ns | 0.7298 ns | 0.6826 ns | 0.0675 |      - |     424 B |
| CopyByArray |    10 |    100 | 32.444 ns | 0.5749 ns | 0.5378 ns | 0.0675 | 0.0001 |     424 B |
|   CopyByFor |   100 |     10 | 11.253 ns | 0.1088 ns | 0.1018 ns | 0.0102 |      - |      64 B |
| CopyByArray |   100 |     10 |  9.738 ns | 0.1242 ns | 0.1162 ns | 0.0102 |      - |      64 B |
|   CopyByFor |   100 |     20 | 18.925 ns | 0.3305 ns | 0.2930 ns | 0.0166 |      - |     104 B |
| CopyByArray |   100 |     20 | 13.211 ns | 0.0924 ns | 0.0819 ns | 0.0166 |      - |     104 B |
|   CopyByFor |   100 |    100 | 87.571 ns | 0.5975 ns | 0.5589 ns | 0.0675 |      - |     424 B |
| CopyByArray |   100 |    100 | 35.169 ns | 0.5966 ns | 0.5581 ns | 0.0675 | 0.0001 |     424 B |
=======
|      Method |     Mean |   Error |  StdDev |  Gen 0 |  Gen 1 | Allocated |
|------------ |---------:|--------:|--------:|-------:|-------:|----------:|
|   CopyByFor | 765.7 ns | 3.72 ns | 3.11 ns | 0.6409 | 0.0095 |      4 KB |
| CopyByArray | 260.6 ns | 3.39 ns | 3.17 ns | 0.6413 | 0.0095 |      4 KB |
| CopyByClone | 250.3 ns | 2.04 ns | 1.70 ns | 0.6390 | 0.0095 |      4 KB |
>>>>>>> 5aef1567e56bf2b8a67c21f94b18f9827c18aaf4
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
<<<<<<< HEAD
        [ArgumentsSource(nameof(ProvideArguments))]
        public object CopyByFor(int start, int length)
        {
            var rawPacketData = TestData;

            var data = new int[length];
            for (int localIndex = 0, rawArrayIndex = start; localIndex < data.Length; localIndex++, rawArrayIndex++)
=======
        public object CopyByFor()
        {
            var rawPacketData = TestData;
            var length = TestData.Length;

            var data = new int[length];
            for (int localIndex = 0, rawArrayIndex = 0; localIndex < data.Length; localIndex++, rawArrayIndex++)
>>>>>>> 5aef1567e56bf2b8a67c21f94b18f9827c18aaf4
            {
                data[localIndex] = rawPacketData[rawArrayIndex];
            }
            return data;
        }

        [Benchmark]
<<<<<<< HEAD
        [ArgumentsSource(nameof(ProvideArguments))]
        public object CopyByArray(int start, int length)
        {
=======
        public object CopyByArray()
        {
            var length = TestData.Length;
            var start = 0;

>>>>>>> 5aef1567e56bf2b8a67c21f94b18f9827c18aaf4
            var rawPacketData = TestData;
            var data = new int[length];
            Array.Copy(rawPacketData,start,data,0, length);
            return data;
        }

<<<<<<< HEAD
<<<<<<< HEAD
=======
        [Benchmark]
        [ArgumentsSource(nameof(ProvideArguments))]
        public object CopyBySpan(int start, int length)
        {
            var rawPacketData = TestData;
            var rawArrayStartIndex = start;
            var data = rawPacketData.AsSpan(rawArrayStartIndex, length).ToArray();
            return data;
        }

>>>>>>> bb71c02beff7a9b257275190b7c6f0c3c2eebe96
        private static readonly int[] TestData;

        public IEnumerable<object[]> ProvideArguments()
        {
            foreach (var start in new[] { 0, 10, 100 })
            {
                foreach (var length in new[] { 10, 20, 100 })
                {
                    yield return new object[] { start, length };
                }
            }
        }
=======
        [Benchmark]
        public object CopyByClone()
        {
            var data = (int[]) TestData.Clone();
            return data;
        }

        private static readonly int[] TestData;

       
>>>>>>> 5aef1567e56bf2b8a67c21f94b18f9827c18aaf4
    }
}
