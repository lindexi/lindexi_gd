// See https://aka.ms/new-console-template for more information

using Microsoft.DotNet.RemoteExecutor;

var code = Console.ReadLine();

RemoteExecutor.Invoke(() =>
{
    File.WriteAllText(@"C:\lindexi\1.txt", code);
});

Console.WriteLine("Hello, World!");
