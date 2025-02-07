// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

var folder = Environment.CurrentDirectory;

var processStartInfo = new ProcessStartInfo("git","fetch")
{
    WorkingDirectory = folder
};

while (true)
{
    Console.WriteLine($"开始拉取 仓库文件夹： {folder}");

    using var process = Process.Start(processStartInfo)!;
    process.WaitForExit();
    var exitCode = process.ExitCode;

    if (exitCode == 0)
    {
        Console.WriteLine($"拉取成功");
        break;
    }
    else
    {
        Console.WriteLine($"拉取失败，错误码 {exitCode}");
    }
}

Console.WriteLine("Hello, World!");
