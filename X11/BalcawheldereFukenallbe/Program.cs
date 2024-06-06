// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

while (!Debugger.IsAttached)
{
    await Task.Delay(100);
}
Debugger.Break();

Console.WriteLine("Hello, World!");
