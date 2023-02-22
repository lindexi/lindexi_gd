// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

Stopwatch stopwatch = Stopwatch.StartNew();
Console.WriteLine("Hello, World!");
await Task.Delay(TimeSpan.FromSeconds(1));
stopwatch.Stop();
Console.WriteLine(stopwatch.Elapsed);
Console.Read();
