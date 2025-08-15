// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

var result = Start();
Console.WriteLine("Hello, World!");

[DllImport("Lib1.dll")]
static extern int Start();