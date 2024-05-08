// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Channels;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BulowukaileFeanayjairwo;

Console.WriteLine(Environment.CommandLine);

//[DllImport("libc.so.6", EntryPoint = "memcpy")]
//static extern void Memcpy(IntPtr a, IntPtr b, IntPtr count);

//unsafe
//{
//    var a = new int[100];
//    for (int i = 0; i < a.Length; i++)
//    {
//        a[i] = i;
//    }

//    var b = new int[a.Length];

//    fixed (int* ap = a)
//    fixed (int* bp = b)
//    {
//        Memcpy(new IntPtr(bp), new IntPtr(ap), a.Length * sizeof(int));
//    }

//    for (var i = 0; i < b.Length; i++)
//    {
//        Console.WriteLine(b[i]);
//    }
//}

//return;

var manualConfig = new ManualConfig()
{
    // 相同的排序，方便用来不同的设备运行对比效果
    Orderer = new DefaultOrderer(SummaryOrderPolicy.Declared),
};
manualConfig.Add(DefaultConfig.Instance);

//BenchmarkRunner.Run(Assembly.GetExecutingAssembly(), manualConfig, args);
BenchmarkRunner.Run<ArrayCopyBenchmark>();
