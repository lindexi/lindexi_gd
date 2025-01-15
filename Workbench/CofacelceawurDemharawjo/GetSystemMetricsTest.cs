using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;

namespace CofacelceawurDemharawjo;

public class GetSystemMetricsTest
{
    [Benchmark]
    public (int X, int Y) Test()
    {
        /*
           BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4391/23H2/2023Update/SunValley3)
           13th Gen Intel Core i7-13700K, 1 CPU, 24 logical and 16 physical cores
           .NET SDK 9.0.100
             [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
             DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


           | Method | Mean     | Error    | StdDev   |
           |------- |---------:|---------:|---------:|
           | Test   | 30.09 ns | 0.035 ns | 0.031 ns |
         */
        // 1,000,000 纳秒 = 1毫秒 ms
        var x = GetSystemMetrics(SystemMetric.SM_CXDOUBLECLK);
        var y = GetSystemMetrics(SystemMetric.SM_CYDOUBLECLK);
        return (x, y);
    }

    public const string LibraryName = "user32";

    /// <summary>
    /// 获取系统度量值
    /// </summary>
    /// <param name="smIndex"></param>
    /// <returns></returns>
    [DllImport(LibraryName)]
    private static extern int GetSystemMetrics(SystemMetric smIndex);

    internal enum SystemMetric
    {
        SM_CXDOUBLECLK = 36, // 0x24
        SM_CYDOUBLECLK = 37, // 0x25
    }
}