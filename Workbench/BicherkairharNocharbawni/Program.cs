// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

Console.WriteLine(Environment.Is64BitProcess);
GetSystemInfoWrite();
Console.WriteLine("Hello, World!");

[DllImport("LightTextEditorPlus.Skia.AotLayer")]
static extern void GetSystemInfoWrite();