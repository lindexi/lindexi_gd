// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

Run();
Console.WriteLine("Hello, World!");

[DllImport("Lib1.dll")]
static extern int Run();