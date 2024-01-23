// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

var filePath = "../Test.txt";

Console.WriteLine($"文件存在 {File.Exists(filePath)}");

Process.Start(new ProcessStartInfo(filePath)
{
    UseShellExecute = true
});