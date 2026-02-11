// See https://aka.ms/new-console-template for more information

using Microsoft.DotNet.RemoteExecutor;

RemoteExecutor.Invoke(() =>
{
    File.WriteAllText(@"C:\lindexi\1.txt", "123");
});

Console.WriteLine("Hello, World!");
