// See https://aka.ms/new-console-template for more information

var memoryInfo = GC.GetGCMemoryInfo();
Console.WriteLine(memoryInfo.HighMemoryLoadThresholdBytes / 1024 / 1024);

Console.WriteLine("Hello, World!");
