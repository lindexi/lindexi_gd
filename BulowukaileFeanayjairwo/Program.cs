﻿// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Threading.Channels;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

Console.WriteLine(Environment.CommandLine);

var manualConfig = new ManualConfig()
{
    // 相同的排序，方便用来不同的设备运行对比效果
    Orderer = new DefaultOrderer(SummaryOrderPolicy.Declared),
};
manualConfig.Add(DefaultConfig.Instance);

BenchmarkRunner.Run(Assembly.GetExecutingAssembly(), manualConfig, args);