// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

var filePath = "../../Documents";

Console.WriteLine($"文件夹存在 {Directory.Exists(filePath)}");

Process.Start(new ProcessStartInfo(filePath)
{
    UseShellExecute = true
});