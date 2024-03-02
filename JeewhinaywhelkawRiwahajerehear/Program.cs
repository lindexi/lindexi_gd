// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

Console.WriteLine("Hello, World!");

[DllImport(@"C:\Program Files (x86)\Foo\vcruntime140.dll")]
static extern void Foo();