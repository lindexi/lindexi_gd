// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

HalQueryRealTimeClock(out var time);
Console.WriteLine("Hello, World!");

[DllImport("ntdll.dll")]
static extern void HalQueryRealTimeClock(out int time);