// See https://aka.ms/new-console-template for more information

using System.Reflection;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

var manualConfig = new ManualConfig()
{
    // 相同的排序，方便用来不同的设备运行对比效果
    Orderer = new DefaultOrderer(SummaryOrderPolicy.Declared),
};
manualConfig.Add(DefaultConfig.Instance);

BenchmarkRunner.Run(Assembly.GetExecutingAssembly(), manualConfig);
